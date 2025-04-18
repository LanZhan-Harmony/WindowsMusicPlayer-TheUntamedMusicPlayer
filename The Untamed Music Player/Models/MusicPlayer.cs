using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace The_Untamed_Music_Player.Models;
public partial class MusicPlayer : ObservableRecipient
{
    private readonly Thickness _defaultMargin = new(0, 20, 0, 20);
    private readonly Thickness _highlightedMargin = new(0, 40, 0, 40);
    private const double _defaultOpacity = 0.5;
    private const double _highlightedOpacity = 1.0;
    private readonly ILocalSettingsService _localSettingsService;

    /// <summary>
    /// 线程锁, 用于限制对Player的访问
    /// </summary>
    private readonly Lock _mediaLock = new();

    /// <summary>
    /// SMTC控件
    /// </summary>
    private readonly SystemMediaTransportControls _systemControls;

    /// <summary>
    /// SMTC显示内容更新器
    /// </summary>
    private readonly SystemMediaTransportControlsDisplayUpdater _displayUpdater;

    /// <summary>
    /// SMTC时间线属性
    /// </summary>
    private readonly SystemMediaTransportControlsTimelineProperties _timelineProperties = new();

    /// <summary>
    /// 线程锁开启状态, true为开启, false为关闭
    /// </summary>
    private bool _lockable = false;

    /// <summary>
    /// 排序方式
    /// </summary>
    private byte _sortMode = 0;

    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    private int _playQueueLength = 0;

    /// <summary>
    /// 播放速度
    /// </summary>
    private double PlaySpeed
    {
        get;
        set
        {
            field = value;
            Player.PlaybackSession.PlaybackRate = value;
        }
    } = 1;

    /// <summary>
    /// 线程计时器
    /// </summary>
    public ThreadPoolTimer? PositionUpdateTimer250ms { get; set; }
    public ThreadPoolTimer? PositionUpdateTimer2000ms { get; set; }

    /// <summary>
    /// 播放队列集合
    /// </summary>
    public ObservableCollection<IBriefMusicInfoBase> PlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    public ObservableCollection<IBriefMusicInfoBase> ShuffledPlayQueue { get; set; } = [];

    /// <summary>
    /// 音乐播放器
    /// </summary>
    public MediaPlayer Player { get; set; } = new() { AudioCategory = MediaPlayerAudioCategory.Media };

    /// <summary>
    /// 歌曲来源模式, 0为本地, 1为网易
    /// </summary>
    public byte SourceMode { get; set; } = 0;

    /// <summary>
    /// 随机播放模式, true为开启, false为关闭.
    /// </summary>
    public bool ShuffleMode { get; set; } = false;

    /// <summary>
    /// 播放队列名
    /// </summary>
    [ObservableProperty]
    public partial string PlayQueueName { get; set; } = "";

    /// <summary>
    /// 当前歌曲在播放队列中的索引
    /// </summary>
    [ObservableProperty]
    public partial int PlayQueueIndex { get; set; }
    partial void OnPlayQueueIndexChanged(int value)
    {
        var repeatOffOrSingle = RepeatMode == 0 || RepeatMode == 2;
        _systemControls.IsPreviousEnabled = !(value == 0 && repeatOffOrSingle);
        _systemControls.IsNextEnabled = !(value == _playQueueLength - 1 && repeatOffOrSingle);
    }

    /// <summary>
    /// 循环播放模式, 0为不循环, 1为列表循环, 2为单曲循环
    /// </summary>
    [ObservableProperty]
    public partial byte RepeatMode { get; set; } = 0;
    partial void OnRepeatModeChanged(byte value)
    {
        var isFirstSong = PlayQueueIndex == 0;
        var isLastSong = PlayQueueIndex == _playQueueLength - 1;
        var isRepeatOffOrSingle = value == 0 || value == 2;
        _systemControls.IsPreviousEnabled = !(isFirstSong && isRepeatOffOrSingle);
        _systemControls.IsNextEnabled = !(isLastSong && isRepeatOffOrSingle);
    }

    /// <summary>
    /// 播放状态, 0为暂停, 1为播放, 2为加载中
    /// </summary>
    [ObservableProperty]
    public partial byte PlayState { get; set; } = 0;

    /// <summary>
    /// 当前播放歌曲
    /// </summary>
    [ObservableProperty]
    public partial IDetailedMusicInfoBase? CurrentMusic { get; set; }
    partial void OnCurrentMusicChanged(IDetailedMusicInfoBase? value)
    {
        if (value!.IsPlayAvailable)
        {
            SetSource(value!.Path);
            _ = UpdateLyric(value!.Lyric);
        }
        else
        {
            HandleSongNotAvailable();
        }
    }

    /// <summary>
    /// 当前播放时间
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan CurrentPlayingTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 当前歌曲总时长
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan TotalPlayingTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 当前播放进度(百分比)
    /// </summary>
    [ObservableProperty]
    public partial double CurrentPosition { get; set; } = 0;

    /// <summary>
    /// 当前音量
    /// </summary>
    [ObservableProperty]
    public partial double CurrentVolume { get; set; } = 100;
    partial void OnCurrentVolumeChanged(double value)
    {
        if (!IsMute)
        {
            Player.Volume = value / 100;
        }
    }

    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;
    partial void OnIsMuteChanged(bool value)
    {
        Player.IsMuted = value;
    }

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    [ObservableProperty]
    public partial int CurrentLyricIndex { get; set; } = 0;

    /// <summary>
    /// 当前歌词内容
    /// </summary>
    [ObservableProperty]
    public partial string CurrentLyricContent { get; set; } = "";

    /// <summary>
    /// 当前歌词切片集合
    /// </summary>
    [ObservableProperty]
    public partial List<LyricSlice> CurrentLyric { get; set; } = [];

    public MusicPlayer()
    {
        _localSettingsService = App.GetService<ILocalSettingsService>();
        Player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        Player.MediaEnded += OnPlaybackStopped;
        Player.MediaFailed += OnPlaybackFailed;
        Player.Volume = CurrentVolume / 100;
        Player.CommandManager.IsEnabled = false;
        _systemControls = Player.SystemMediaTransportControls;
        _displayUpdater = _systemControls.DisplayUpdater;
        _displayUpdater.Type = MediaPlaybackType.Music;
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += SystemControls_ButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;
        LoadCurrentStateAsync();
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="path"></param>
    public async void PlaySongByInfo(IBriefMusicInfoBase info)
    {
        Stop();
        CurrentMusic = await IDetailedMusicInfoBase.CreateDetailedMusicInfoAsync(info, SourceMode);
        _systemControls.IsPlayEnabled = true;
        _systemControls.IsPauseEnabled = true;
        PlayQueueIndex = info.PlayQueueIndex;
        Play();
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isLast"></param>
    private async void PlaySongByIndex(int index, bool isLast = false)
    {
        Stop();
        var songToPlay = ShuffleMode ? ShuffledPlayQueue[index] : PlayQueue[index];
        CurrentMusic = await IDetailedMusicInfoBase.CreateDetailedMusicInfoAsync(songToPlay, SourceMode);
        PlayQueueIndex = isLast ? 0 : index;
        _systemControls.IsPlayEnabled = true;
        _systemControls.IsPauseEnabled = true;
        if (!isLast)
        {
            Play();
        }
    }

    /// <summary>
    /// 将歌曲添加到下一首播放
    /// </summary>
    /// <param name="info"></param>
    public void AddSongToNextPlay(IBriefMusicInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        queue.Insert(insertIndex, (IBriefMusicInfoBase)info.Clone());
        _playQueueLength++;
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex; i < queue.Count; i++)
        {
            queue[i].PlayQueueIndex = i;
        }
    }

    /// <summary>
    /// 将歌曲列表添加到下一首播放
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToNextPlay(IEnumerable<IBriefMusicInfoBase> songs)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        foreach (var song in songs)
        {
            queue.Insert(insertIndex, (IBriefMusicInfoBase)song.Clone());
            insertIndex++;
        }
        _playQueueLength += songs.Count();
        // 仅更新从插入位置之后的索引
        for (var i = insertIndex - songs.Count(); i < queue.Count; i++)
        {
            queue[i].PlayQueueIndex = i;
        }
    }

    /// <summary>
    /// 从播放队列中移除歌曲
    /// </summary>
    /// <param name="info"></param>
    public async Task RemoveSong(IBriefMusicInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        int newIndex;
        if (index == PlayQueueIndex) // 如果删除的歌曲正好是当前播放歌曲
        {
            Stop();
            newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[newIndex] : PlayQueue[newIndex];
            CurrentMusic = await IDetailedMusicInfoBase.CreateDetailedMusicInfoAsync(songToPlay, SourceMode);
            PlayQueueIndex = newIndex == 0 ? 0 : newIndex - 1;
            _systemControls.IsPauseEnabled = true;
            _systemControls.IsPlayEnabled = true;
            if (PlayState != 0)
            {
                Play();
            }
        }
        else if (index < PlayQueueIndex) // 删除歌曲在当前播放歌曲之前时，当前索引前移1位
        {
            PlayQueueIndex--;
        }

        // 从队列中移除歌曲并更新后续歌曲的索引
        queue.RemoveAt(index);
        _playQueueLength--;
        for (var i = index; i < queue.Count; i++)
        {
            queue[i].PlayQueueIndex = i;
        }
        OnPropertyChanged(nameof(PlayQueueIndex));

        if (queue.Count == 0)
        {
            ClearPlayQueue();
        }
    }

    /// <summary>
    /// 将歌曲上移
    /// </summary>
    /// <param name="info"></param>
    public void MoveUpSong(IBriefMusicInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        if (index > 0)
        {
            (queue[index - 1], queue[index]) = (queue[index], queue[index - 1]);
            queue[index].PlayQueueIndex = index;
            queue[index - 1].PlayQueueIndex = index - 1;
            if (index == PlayQueueIndex)
            {
                PlayQueueIndex--;
            }
        }
    }

    /// <summary>
    /// 将歌曲下移
    /// </summary>
    /// <param name="info"></param>
    public void MoveDownSong(IBriefMusicInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        if (index < queue.Count - 1)
        {
            (queue[index + 1], queue[index]) = (queue[index], queue[index + 1]);
            queue[index].PlayQueueIndex = index;
            queue[index + 1].PlayQueueIndex = index + 1;
            if (index == PlayQueueIndex)
            {
                PlayQueueIndex++;
            }
        }
    }

    /// <summary>
    /// 为播放器设置音乐源
    /// </summary>
    /// <param name="path"></param>
    private async void SetSource(string path)
    {
        try
        {
            Player.Source = null;
            if (SourceMode == 0)
            {
                var mediaFile = await StorageFile.GetFileFromPathAsync(path);
                Player.Source = MediaSource.CreateFromStorageFile(mediaFile);
            }
            else
            {
                Player.Source = MediaSource.CreateFromUri(new Uri(path));
            }
            Player.PlaybackSession.PlaybackRate = PlaySpeed;
            TotalPlayingTime = Player.PlaybackSession.NaturalDuration;
            _displayUpdater.MusicProperties.Title = CurrentMusic!.Title;
            _displayUpdater.MusicProperties.Artist = CurrentMusic.ArtistsStr == "未知艺术家" ? "" : CurrentMusic.ArtistsStr;
            _timelineProperties.MaxSeekTime = Player.PlaybackSession.NaturalDuration;
            _timelineProperties.EndTime = Player.PlaybackSession.NaturalDuration;
            PositionUpdateTimer250ms = ThreadPoolTimer.CreatePeriodicTimer(UpdateTimerHandler250ms, TimeSpan.FromMilliseconds(250));
            PositionUpdateTimer2000ms = ThreadPoolTimer.CreatePeriodicTimer(UpdateTimerHandler2000ms, TimeSpan.FromMilliseconds(2000));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }

        if (CurrentMusic!.Cover is not null)
        {
            if (SourceMode == 0)
            {
                try
                {
                    var info = (DetailedMusicInfo)CurrentMusic;
                    var tempFolder = ApplicationData.Current.TemporaryFolder;
                    var coverFileName = "Cover.jpg";
                    var coverFile = await tempFolder.CreateFileAsync(coverFileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(coverFile, info.CoverBuffer);
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromFile(coverFile);
                }
                catch
                {
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/NoCover.png"));
                }
            }
            else
            {
                try
                {
                    var info = (IDetailedOnlineMusicInfo)CurrentMusic;
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(info.CoverUrl!));
                }
                catch { }
            }
        }
        else
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/NoCover.png"));
        }
        _displayUpdater.Update();
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public void SetPlayList(string name, IEnumerable<IBriefMusicInfoBase> list, byte sourceMode = 0, byte sortMode = 0)
    {
        if (PlayQueue.Count != list.Count() || PlayQueueName != name || _sortMode != sortMode || SourceMode != sourceMode)
        {
            _sortMode = sortMode;
            SourceMode = sourceMode;
            PlayQueueName = name;
            PlayQueue = [.. list];
            _playQueueLength = list.Count();

            bool hasMusics;
            if (!ShuffleMode)
            {
                // 更新播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < PlayQueue.Count; i++)
                {
                    PlayQueue[i].PlayQueueIndex = i;
                }
                hasMusics = PlayQueue.Any();
            }
            else
            {
                UpdateShufflePlayQueue();
                // 更新随机播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < ShuffledPlayQueue.Count; i++)
                {
                    ShuffledPlayQueue[i].PlayQueueIndex = i;
                }
                if (CurrentMusic is not null)
                {
                    // 更新当前歌曲在随机播放队列中的索引
                    PlayQueueIndex = ShuffledPlayQueue.FirstOrDefault(info => CurrentMusic.IsOnline
                    ? ((IBriefOnlineMusicInfo)info).ID == ((IDetailedOnlineMusicInfo)CurrentMusic).ID
                    : info.Path == CurrentMusic.Path)?.PlayQueueIndex ?? 0;
                }
                hasMusics = ShuffledPlayQueue.Any();
            }
            if (Data.RootPlayBarViewModel is not null)
            {
                Data.RootPlayBarViewModel.ButtonVisibility = hasMusics ? Visibility.Visible : Visibility.Collapsed;
                Data.RootPlayBarViewModel.Availability = hasMusics;
            }
        }
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler250ms(ThreadPoolTimer timer)
    {
        lock (_mediaLock)
        {
            try
            {
                if (Player.PlaybackSession is null || _lockable || Player.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                {
                    return;
                }
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    CurrentPlayingTime = Player.PlaybackSession.Position;
                    TotalPlayingTime = Player.PlaybackSession.NaturalDuration;
                    if (TotalPlayingTime.TotalMilliseconds > 0)
                    {
                        CurrentPosition = 100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
                    }
                });

                var dispatcherQueue = Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
                if (CurrentLyric.Count > 0)
                {
                    dispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        CurrentLyricIndex = GetCurrentLyricIndex(Player.PlaybackSession.Position.TotalMilliseconds);
                        CurrentLyricContent = CurrentLyric[CurrentLyricIndex].Content;
                    });
                }
                else
                {
                    dispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        CurrentLyricIndex = 0;
                        CurrentLyricContent = "";
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
    private void UpdateTimerHandler2000ms(ThreadPoolTimer timer)
    {
        lock (_mediaLock)
        {
            try
            {
                if (Player.PlaybackSession is null || _lockable || Player.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                {
                    return;
                }
                _timelineProperties.Position = Player.PlaybackSession.Position;
                _systemControls.UpdateTimelineProperties(_timelineProperties);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    /// <summary>
    /// 获取当前歌词切片索引
    /// </summary>
    /// <param name="currentTime"></param>
    /// <returns></returns>
    public int GetCurrentLyricIndex(double currentTime)
    {
        for (var i = 0; i < CurrentLyric.Count; i++)
        {
            if (CurrentLyric[i].Time > currentTime)
            {
                return i > 0 ? i - 1 : 0;
            }
        }
        return CurrentLyric.Count - 1;
    }

    /// <summary>
    /// 播放状态改变事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        try
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.None:
                    break;
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Buffering:
                    Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 2;
                    });
                    break;
                case MediaPlaybackState.Playing:
                    Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 1;
                        _systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    });
                    break;
                case MediaPlaybackState.Paused:
                    Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 0;
                        _systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    });
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 播放结束事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnPlaybackStopped(MediaPlayer sender, object args)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (sender.PlaybackSession.PlaybackState == MediaPlaybackState.Paused && !_lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByInfo(CurrentMusic!);
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    /// <summary>
    /// 播放失败事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnPlaybackFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || SourceMode != 0)
            {
                Stop();
            }
            else
            {
                PlayNextSong();
            }
        });
    }

    private void HandleSongNotAvailable()
    {
        if (PlayQueue.Count == 0)
        {
            return;
        }
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || SourceMode != 0)
            {
                Stop();
            }
            else
            {
                PlayNextSong();
            }
        });
    }

    private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Play:
                PlayPauseUpdate();
                break;
            case SystemMediaTransportControlsButton.Pause:
                PlayPauseUpdate();
                break;
            case SystemMediaTransportControlsButton.Previous:// 注意: 必须在UI线程中调用
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, PlayPreviousSong);
                break;
            case SystemMediaTransportControlsButton.Next:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, PlayNextSong);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 静音按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        IsMute = !IsMute;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        Player?.Play();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        Player?.Pause();
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        Player?.Pause();
        CurrentPlayingTime = TimeSpan.Zero;
        CurrentPosition = 0;
        CurrentLyricIndex = 0;
        CurrentLyricContent = "";
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer2000ms?.Cancel();
        PositionUpdateTimer250ms = null;
        PositionUpdateTimer2000ms = null;
    }

    /// <summary>
    /// 播放上一曲
    /// </summary>
    public void PlayPreviousSong()
    {
        try
        {
            var newIndex = PlayQueueIndex > 0 ? PlayQueueIndex - 1 : PlayQueueIndex;

            if (RepeatMode == 1) // 列表循环
            {
                newIndex = (PlayQueueIndex + _playQueueLength - 1) % _playQueueLength;
            }

            PlaySongByIndex(newIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 播放下一曲
    /// </summary>
    public void PlayNextSong()
    {
        try
        {
            var newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var isLast = PlayQueueIndex >= _playQueueLength - 1;

            if (RepeatMode == 1) // 列表循环
            {
                newIndex = (PlayQueueIndex + 1) % _playQueueLength;
                isLast = false;
            }

            PlaySongByIndex(newIndex, isLast);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }


    /// <summary>
    /// 播放按钮更新
    /// </summary>
    public void PlayPauseUpdate()
    {
        if (PlayState == 1 || PlayState == 2)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    /// <summary>
    /// 随机播放模式更新
    /// </summary>
    public void ShuffleModeUpdate()
    {
        ShuffleMode = !ShuffleMode;
        if (ShuffleMode)
        {
            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].PlayQueueIndex = i;
            }
            if (CurrentMusic is not null)
            {
                PlayQueueIndex = ShuffledPlayQueue.FirstOrDefault(info => CurrentMusic.IsOnline
                ? ((IBriefOnlineMusicInfo)info).ID == ((IDetailedOnlineMusicInfo)CurrentMusic).ID
                : info.Path == CurrentMusic.Path)?.PlayQueueIndex ?? 0;
            }
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                PlayQueue[i].PlayQueueIndex = i;
            }
            if (CurrentMusic is not null)
            {
                PlayQueueIndex = PlayQueue.FirstOrDefault(info => CurrentMusic.IsOnline
                ? ((IBriefOnlineMusicInfo)info).ID == ((IDetailedOnlineMusicInfo)CurrentMusic).ID
                : info.Path == CurrentMusic.Path)?.PlayQueueIndex ?? 0;
            }
        }
        OnPropertyChanged(nameof(ShuffleMode));
    }

    /// <summary>
    /// 循环播放模式更新
    /// </summary>
    public void RepeatModeUpdate()
    {
        RepeatMode = (byte)((RepeatMode + 1) % 3);
    }

    /// <summary>
    /// 随机播放队列更新
    /// </summary>
    /// <returns></returns>
    public void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = new ObservableCollection<IBriefMusicInfoBase>(
            [.. PlayQueue.OrderBy(x => Guid.NewGuid())]);
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        _systemControls.IsPlayEnabled = false;
        _systemControls.IsPauseEnabled = false;
        _systemControls.IsPreviousEnabled = false;
        _systemControls.IsNextEnabled = false;
        TotalPlayingTime = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        CurrentLyric.Clear();
        PlayQueueName = "";
        PlayQueueIndex = 0;
        _playQueueLength = 0;
        if (Data.RootPlayBarViewModel is not null)
        {
            Data.RootPlayBarViewModel.ButtonVisibility = PlayQueue.Any() ? Visibility.Visible : Visibility.Collapsed;
            Data.RootPlayBarViewModel.Availability = PlayQueue.Any();
        }
    }

    /// <summary>
    /// 按下滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressLock(object sender, PointerRoutedEventArgs e)
    {
        _lockable = true;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100);
    }

    /// <summary>
    /// 滑动滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SliderUpdate(object sender, PointerRoutedEventArgs e)
    {
        CurrentPlayingTime = TimeSpan.FromMilliseconds(((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100);
        CurrentLyricIndex = GetCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
    }

    /// <summary>
    /// 松开滑动条更新播放进度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressUpdate(object sender, PointerRoutedEventArgs e)
    {
        Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100);
        CurrentPlayingTime = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        TotalPlayingTime = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        _lockable = false;
    }

    /// <summary>
    /// 点击歌词更新播放进度
    /// </summary>
    /// <param name="time"></param>
    public void LyricProgressUpdate(double time)
    {
        _lockable = true;
        Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(time);
        CurrentPlayingTime = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        TotalPlayingTime = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        _lockable = false;
    }

    public void SkipBack10sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        if (Player.PlaybackSession.Position.TotalMilliseconds - 10000 < 0)
        {
            Player.PlaybackSession.Position = TimeSpan.Zero;
        }
        else
        {
            Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(Player.PlaybackSession.Position.TotalMilliseconds - 10000);
        }
        CurrentPlayingTime = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        TotalPlayingTime = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        _lockable = false;
    }

    public void SkipForw30sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        if (Player.PlaybackSession.Position.TotalMilliseconds + 30000 > TotalPlayingTime.TotalMilliseconds)
        {
            Player.PlaybackSession.Position = TotalPlayingTime;
        }
        else
        {
            Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(Player.PlaybackSession.Position.TotalMilliseconds + 30000);
        }
        CurrentPlayingTime = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        TotalPlayingTime = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        _lockable = false;
    }

    public void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listview && listview.SelectedIndex is int selectedIndex)
        {
            PlaySpeed = selectedIndex switch
            {
                0 => 0.25,
                1 => 0.5,
                2 => 1,
                3 => 1.5,
                4 => 2,
                _ => 1
            };
        }
    }

    public void SpeedListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listview)
        {
            listview.SelectedIndex = PlaySpeed switch
            {
                0.25 => 0,
                0.5 => 1,
                1 => 2,
                1.5 => 3,
                2 => 4,
                _ => 2
            };
        }
    }

    public async Task UpdateLyric(string lyric)
    {
        CurrentLyric = await LyricSlice.GetLyricSlices(lyric);
    }

    /// <summary>
    /// 获取歌词字体大小
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public double GetLyricFont(double itemTime, int currentLyricIndex, double mainWindowWidth)
    {
        var defaultFontSize = mainWindowWidth <= 1000 ? 16.0 : 20.0;
        var highlightedFontSize = mainWindowWidth <= 1000 ? 24.0 : 50.0;
        return itemTime == CurrentLyric[currentLyricIndex].Time ? highlightedFontSize : defaultFontSize;
    }

    /// <summary>
    /// 获取歌词边距
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public Thickness GetLyricMargin(double itemTime, int currentLyricIndex)
    {
        return itemTime == CurrentLyric[currentLyricIndex].Time ? _highlightedMargin : _defaultMargin;
    }

    /// <summary>
    /// 获取歌词透明度 
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public double GetLyricOpacity(double itemTime, int currentLyricIndex)
    {
        return itemTime == CurrentLyric[currentLyricIndex].Time ? _highlightedOpacity : _defaultOpacity;
    }

    /// <summary>
    /// 获取播放队列
    /// </summary>
    /// <param name="PlayQueueName"></param>
    /// <param name="ShuffleMode"></param>
    /// <returns></returns>
    public ObservableCollection<IBriefMusicInfoBase> GetPlayQueue(string PlayQueueName, bool ShuffleMode) => ShuffleMode ? ShuffledPlayQueue : PlayQueue;


    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        /*var playqueuepaths = PlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("PlayQueuePaths", playqueuepaths);
        var shuffledplayqueuepaths = ShuffledPlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("ShuffledPlayQueuePaths", shuffledplayqueuepaths);
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);*/
        await _localSettingsService.SaveSettingAsync("SourceMode", SourceMode);
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
        if (CurrentMusic is not null)
        {
            await _localSettingsService.SaveSettingAsync("CurrentMusic", CurrentMusic.Path);
        }
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        /*var playqueuepaths = await _localSettingsService.ReadSettingAsync<List<string>>("PlayQueuePaths");
        if (playqueuepaths is not null)
        {
            PlayQueue.Clear();
            foreach (var path in playqueuepaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var musicInfo = new BriefMusicInfo(path);
                    PlayQueue.Add(musicInfo);
                }
            }
        }
        var shuffledplayqueuepaths = await _localSettingsService.ReadSettingAsync<List<string>>("ShuffledPlayQueuePaths");
        if (shuffledplayqueuepaths is not null)
        {
            ShuffledPlayQueue.Clear();
            foreach (var path in shuffledplayqueuepaths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var musicInfo = new BriefMusicInfo(path);
                    ShuffledPlayQueue.Add(musicInfo);
                }
            }
        }
        PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>("PlayQueueIndex");*/
        SourceMode = await _localSettingsService.ReadSettingAsync<byte>("SourceMode");
        ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
        RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
        IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
        var currentMusicPath = await _localSettingsService.ReadSettingAsync<string>("CurrentMusic");
        if (!string.IsNullOrEmpty(currentMusicPath) && SourceMode == 0)
        {
            CurrentMusic = new DetailedMusicInfo(currentMusicPath);
        }
        if (Data.NotFirstUsed)
        {
            CurrentVolume = await _localSettingsService.ReadSettingAsync<double>("CurrentVolume");
            PlaySpeed = await _localSettingsService.ReadSettingAsync<double>("PlaySpeed");
        }
        else
        {
            CurrentVolume = 100;
            PlaySpeed = 1;
        }
        /*if (Data.RootPlayBarViewModel is not null)
        {
            Data.RootPlayBarViewModel.ButtonVisibility = PlayQueue.Any() ? Visibility.Visible : Visibility.Collapsed;
            Data.RootPlayBarViewModel.Availability = PlayQueue.Any();
        }*/
    }
}