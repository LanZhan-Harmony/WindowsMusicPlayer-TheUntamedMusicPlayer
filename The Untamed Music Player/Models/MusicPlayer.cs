using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using Windows.Media;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace The_Untamed_Music_Player.Models;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _vlcPlayer;
    private Media? _currentMedia;
    private readonly Windows.Media.Playback.MediaPlayer? _windowsMediaPlayer;

    /// <summary>
    /// 用于SMTC显示封面图片的流
    /// </summary>
    private static InMemoryRandomAccessStream? _currentCoverStream = null!;

    /// <summary>
    /// 当前播放的歌曲简要版
    /// </summary>
    private IBriefSongInfoBase? _currentBriefSong;

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
    /// 播放失败次数
    /// </summary>
    private byte _failedCount = 0;

    /// <summary>
    /// 排序方式
    /// </summary>
    private byte _sortMode = 0;

    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    private int _playQueueLength = 0;

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    private int _currentLyricIndex = 0;

    /// <summary>
    /// 播放速度
    /// </summary>
    private double PlaySpeed
    {
        get;
        set
        {
            field = value;
            _vlcPlayer.SetRate((float)value);
        }
    } = 1;

    /// <summary>
    /// 线程计时器
    /// </summary>
    public ThreadPoolTimer? PositionUpdateTimer250ms { get; set; }

    /// <summary>
    /// 播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IBriefSongInfoBase> PlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IBriefSongInfoBase> ShuffledPlayQueue { get; set; } = [];

    /// <summary>
    /// 歌曲来源模式, 0为本地, 1为网易
    /// </summary>
    public byte SourceMode { get; set; } = 0;

    /// <summary>
    /// 随机播放模式, true为开启, false为关闭.
    /// </summary>
    [ObservableProperty]
    public partial bool ShuffleMode { get; set; } = false;

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
    public partial IDetailedSongInfoBase? CurrentSong { get; set; }

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
    public partial double CurrentPosition { get; set; } = 0.0;

    /// <summary>
    /// 当前音量
    /// </summary>
    [ObservableProperty]
    public partial double CurrentVolume { get; set; } = 100.0;

    partial void OnCurrentVolumeChanged(double value)
    {
        if (!IsMute)
        {
            _vlcPlayer.Volume = (int)value;
        }
    }

    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    partial void OnIsMuteChanged(bool value)
    {
        _vlcPlayer.Mute = value;
    }

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

        // 初始化 LibVLC
        Core.Initialize();
        _libVlc = new LibVLC();
        _vlcPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);

        // 创建 LibVLC 事件处理器
        _vlcPlayer.Playing += OnVlcPlaying;
        _vlcPlayer.Paused += OnVlcPaused;
        _vlcPlayer.Stopped += OnVlcStopped;
        _vlcPlayer.EndReached += OnVlcEndReached;
        _vlcPlayer.EncounteredError += OnVlcError;
        _vlcPlayer.TimeChanged += OnVlcTimeChanged;
        _vlcPlayer.LengthChanged += OnVlcLengthChanged;
        _vlcPlayer.Opening += OnVlcOpening;
        _vlcPlayer.Buffering += OnVlcBuffering;

        _vlcPlayer.Volume = (int)CurrentVolume;
        _vlcPlayer.Mute = IsMute;

        _windowsMediaPlayer = new();
        _systemControls = _windowsMediaPlayer.SystemMediaTransportControls;
        _displayUpdater = _systemControls.DisplayUpdater;
        _displayUpdater.Type = MediaPlaybackType.Music;
        _systemControls.IsEnabled = true;
        _systemControls.ButtonPressed += SystemControls_ButtonPressed;
        _timelineProperties.StartTime = TimeSpan.Zero;
        _timelineProperties.MinSeekTime = TimeSpan.Zero;

        LoadCurrentStateAsync();
    }

    // LibVLC 事件处理函数
    private void OnVlcOpening(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => PlayState = 2
        );
    }

    private void OnVlcBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                PlayState = 2;
                var bufferingProgress = e.Cache / 100d;
                if (bufferingProgress == 1.0)
                {
                    PlayState = 1;
                }
            }
        );
    }

    private void OnVlcPlaying(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                PlayState = 1;
                _systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
        );
    }

    private void OnVlcPaused(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                PlayState = 0;
                _systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        );
    }

    private void OnVlcStopped(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                PlayState = 0;
                _systemControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
            }
        );
    }

    private void OnVlcEndReached(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByInfo(_currentBriefSong!);
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    private void OnVlcError(object? sender, EventArgs e)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || SourceMode != 0)
            {
                Stop();
            }
            else
            {
                _currentBriefSong!.IsPlayAvailable = false;
                _failedCount++;
                if (_failedCount > 2)
                {
                    _failedCount = 0;
                    Stop();
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    private void OnVlcTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        if (_lockable)
        {
            return;
        }

        var newTime = TimeSpan.FromMilliseconds(e.Time);
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            CurrentPlayingTime = newTime;
            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
        });

        var dispatcherQueue =
            Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
        if (CurrentLyric.Count > 0)
        {
            dispatcherQueue?.TryEnqueue(() =>
            {
                UpdateCurrentLyricIndex(e.Time);
            });
        }

        _timelineProperties.Position = newTime;
        _systemControls.UpdateTimelineProperties(_timelineProperties);
    }

    private void OnVlcLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        var duration = TimeSpan.FromMilliseconds(e.Length);
        App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            TotalPlayingTime = duration;
            _timelineProperties.MaxSeekTime = duration;
            _timelineProperties.EndTime = duration;
        });
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="path"></param>
    public async void PlaySongByInfo(IBriefSongInfoBase info)
    {
        Stop();
        _currentBriefSong = info;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
        PlayQueueIndex = info.PlayQueueIndex;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _systemControls.IsPlayEnabled = true;
            _systemControls.IsPauseEnabled = true;
            Play();
        }
        else
        {
            HandleSongNotAvailable();
        }
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
        _currentBriefSong = songToPlay;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay);
        PlayQueueIndex = isLast ? 0 : index;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _systemControls.IsPlayEnabled = true;
            _systemControls.IsPauseEnabled = true;
            if (!isLast)
            {
                Play();
            }
        }
        else
        {
            HandleSongNotAvailable();
        }
    }

    /// <summary>
    /// 将歌曲添加到下一首播放
    /// </summary>
    /// <param name="info"></param>
    public void AddSongToNextPlay(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        queue.Insert(insertIndex, (IBriefSongInfoBase)info.Clone());
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
    public void AddSongsToNextPlay(IEnumerable<IBriefSongInfoBase> songs)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var insertIndex = PlayQueueIndex + 1;
        foreach (var song in songs)
        {
            queue.Insert(insertIndex, (IBriefSongInfoBase)song.Clone());
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
    public async Task RemoveSong(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.PlayQueueIndex;
        int newIndex;
        if (index == PlayQueueIndex) // 如果删除的歌曲正好是当前播放歌曲
        {
            Stop();
            newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[newIndex] : PlayQueue[newIndex];
            _currentBriefSong = songToPlay;
            CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay);
            PlayQueueIndex = newIndex == 0 ? 0 : newIndex - 1;
            if (PlayState != 0)
            {
                if (CurrentSong!.IsPlayAvailable)
                {
                    await SetSource(CurrentSong!.Path);
                    _ = UpdateLyric(CurrentSong!.Lyric);
                    _systemControls.IsPauseEnabled = true;
                    _systemControls.IsPlayEnabled = true;
                    Play();
                }
                else
                {
                    HandleSongNotAvailable();
                }
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
    public void MoveUpSong(IBriefSongInfoBase info)
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
    public void MoveDownSong(IBriefSongInfoBase info)
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
    private async Task SetSource(string path)
    {
        try
        {
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Visible;
            Data.RootPlayBarViewModel?.Availability = true;

            _currentMedia?.Dispose();

            if (SourceMode == 0)
            {
                _currentMedia = new Media(_libVlc, path, FromType.FromPath);
            }
            else
            {
                _currentMedia = new Media(_libVlc, path, FromType.FromLocation);
            }

            _vlcPlayer.Media = _currentMedia;
            _vlcPlayer.SetRate((float)PlaySpeed);

            _displayUpdater.MusicProperties.Title = CurrentSong!.Title;
            _displayUpdater.MusicProperties.Artist =
                CurrentSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
                    ? ""
                    : CurrentSong.ArtistsStr;

            PositionUpdateTimer250ms = ThreadPoolTimer.CreatePeriodicTimer(
                UpdateTimerHandler250ms,
                TimeSpan.FromMilliseconds(250)
            );

            if (ShuffleMode)
            {
                // 更新当前歌曲在随机播放队列中的索引
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }

        if (CurrentSong!.Cover is not null)
        {
            if (SourceMode == 0)
            {
                try
                {
                    var info = (DetailedLocalSongInfo)CurrentSong;
                    _currentCoverStream?.Dispose();
                    _currentCoverStream = new InMemoryRandomAccessStream();
                    await _currentCoverStream.WriteAsync(info.CoverBuffer.AsBuffer());
                    _currentCoverStream.Seek(0);
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(
                        _currentCoverStream
                    );
                }
                catch
                {
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                        new Uri("ms-appx:///Assets/NoCover.png")
                    );
                }
            }
            else
            {
                try
                {
                    var info = (IDetailedOnlineSongInfo)CurrentSong;
                    _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                        new Uri(info.CoverPath!)
                    );
                }
                catch { }
            }
        }
        else
        {
            _displayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                new Uri("ms-appx:///Assets/NoCover.png")
            );
        }
        _displayUpdater.Update();
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public void SetPlayList(
        string name,
        IEnumerable<IBriefSongInfoBase> list,
        byte sourceMode = 0,
        byte sortMode = 0
    )
    {
        if (
            PlayQueue.Count != list.Count()
            || PlayQueueName != name
            || _sortMode != sortMode
            || SourceMode != sourceMode
        )
        {
            _sortMode = sortMode;
            SourceMode = sourceMode;
            PlayQueueName = name;
            PlayQueue = [.. list];
            _playQueueLength = list.Count();

            if (!ShuffleMode)
            {
                // 更新播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < PlayQueue.Count; i++)
                {
                    PlayQueue[i].PlayQueueIndex = i;
                }
            }
            else
            {
                UpdateShufflePlayQueue();
                // 更新随机播放队列中每首歌曲的 PlayQueueIndex 为实际位置
                for (var i = 0; i < ShuffledPlayQueue.Count; i++)
                {
                    ShuffledPlayQueue[i].PlayQueueIndex = i;
                }
            }
            FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
        }
    }

    public void SetShuffledPlayList(
        string name,
        IEnumerable<IBriefSongInfoBase> list,
        byte sourceMode = 0,
        byte sortMode = 0
    )
    {
        if (
            ShuffleMode == false
            || PlayQueue.Count != list.Count()
            || PlayQueueName != name
            || _sortMode != sortMode
            || SourceMode != sourceMode
        )
        {
            _sortMode = sortMode;
            SourceMode = sourceMode;
            ShuffleMode = true;
            PlayQueueName = name;
            PlayQueue = [.. list];
            _playQueueLength = list.Count();

            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].PlayQueueIndex = i;
            }
        }
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler250ms(ThreadPoolTimer timer)
    {
        try
        {
            if (_vlcPlayer == null || _lockable || _vlcPlayer.State != VLCState.Playing)
            {
                return;
            }

            var currentTime = _vlcPlayer.Time;
            var totalTime = _vlcPlayer.Length;

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                CurrentPlayingTime = TimeSpan.FromMilliseconds(currentTime);
                TotalPlayingTime = TimeSpan.FromMilliseconds(totalTime);
                if (TotalPlayingTime.TotalMilliseconds > 0)
                {
                    CurrentPosition =
                        100
                        * (
                            CurrentPlayingTime.TotalMilliseconds
                            / TotalPlayingTime.TotalMilliseconds
                        );
                }
            });

            var dispatcherQueue =
                Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
            if (CurrentLyric.Count > 0)
            {
                dispatcherQueue?.TryEnqueue(() =>
                {
                    UpdateCurrentLyricIndex(currentTime);
                });
            }

            _timelineProperties.Position = TimeSpan.FromMilliseconds(currentTime);
            _systemControls.UpdateTimelineProperties(_timelineProperties);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
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
    /// 更新当前歌词索引和状态
    /// </summary>
    /// <param name="currentTime">当前播放时间（毫秒）</param>
    private void UpdateCurrentLyricIndex(double currentTime)
    {
        if (CurrentLyric.Count == 0)
        {
            return;
        }

        var newIndex = GetCurrentLyricIndex(currentTime);

        if (newIndex != _currentLyricIndex)
        {
            // 更新旧歌词状态
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = false;
            }

            _currentLyricIndex = newIndex;

            // 更新新歌词状态
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = true;
                CurrentLyricContent = CurrentLyric[_currentLyricIndex].Content;
            }
        }
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
                _failedCount++;
                if (_failedCount > 2)
                {
                    _failedCount = 0;
                    Stop();
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    private void SystemControls_ButtonPressed(
        SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args
    )
    {
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Play:
                PlayPauseUpdate();
                break;
            case SystemMediaTransportControlsButton.Pause:
                PlayPauseUpdate();
                break;
            case SystemMediaTransportControlsButton.Previous: // 注意: 必须在UI线程中调用
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayPreviousSong
                );
                break;
            case SystemMediaTransportControlsButton.Next:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayNextSong
                );
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
        _vlcPlayer?.Play();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        _vlcPlayer?.Pause();
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        _vlcPlayer?.Stop();
        CurrentPlayingTime = TimeSpan.Zero;
        CurrentPosition = 0;
        _currentLyricIndex = 0;
        CurrentLyricContent = "";
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer250ms = null;
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
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                PlayQueue[i].PlayQueueIndex = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    PlayQueue
                        .FirstOrDefault(info =>
                            CurrentSong.IsOnline
                                ? ((IBriefOnlineSongInfo)info).ID
                                    == ((IDetailedOnlineSongInfo)CurrentSong).ID
                                : info.Path == CurrentSong.Path
                        )
                        ?.PlayQueueIndex ?? 0;
            }
        }
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
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
        ShuffledPlayQueue = new ObservableCollection<IBriefSongInfoBase>(
            [.. PlayQueue.OrderBy(x => Guid.NewGuid())]
        );
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        _currentBriefSong = null;
        CurrentSong = null;
        CurrentPlayingTime = TimeSpan.Zero;
        TotalPlayingTime = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        CurrentLyric.Clear();
        PlayQueueName = "";
        PlayQueueIndex = 0;
        _playQueueLength = 0;
        Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Collapsed;
        Data.RootPlayBarViewModel?.Availability = false;
        _systemControls.IsPlayEnabled = false;
        _systemControls.IsPauseEnabled = false;
        _systemControls.IsPreviousEnabled = false;
        _systemControls.IsNextEnabled = false;
        FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 按下滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressLock(object sender, PointerRoutedEventArgs e)
    {
        _lockable = true;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
    }

    /// <summary>
    /// 滑动滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SliderUpdate(object sender, PointerRoutedEventArgs e)
    {
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
        UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
    }

    /// <summary>
    /// 松开滑动条更新播放进度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressUpdate(object sender, PointerRoutedEventArgs e)
    {
        var newPosition = ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100;
        _vlcPlayer.Time = (long)newPosition;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(newPosition);
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition =
                100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        UpdateCurrentLyricIndex(newPosition);
        _lockable = false;
    }

    /// <summary>
    /// 点击歌词更新播放进度
    /// </summary>
    /// <param name="time"></param>
    public void LyricProgressUpdate(double time)
    {
        _lockable = true;
        _vlcPlayer.Time = (long)time;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(time);
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition =
                100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        UpdateCurrentLyricIndex(time);
        _lockable = false;
    }

    public void SkipBack10sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        var currentTime = _vlcPlayer.Time;
        var newTime = Math.Max(0, currentTime - 10000);
        _vlcPlayer.Time = newTime;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(newTime);
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition =
                100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        UpdateCurrentLyricIndex(newTime);
        _lockable = false;
    }

    public void SkipForw30sButton_Click(object sender, RoutedEventArgs e)
    {
        _lockable = true;
        var currentTime = _vlcPlayer.Time;
        var totalTime = _vlcPlayer.Length;
        var newTime = Math.Min(totalTime, currentTime + 30000);
        _vlcPlayer.Time = newTime;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(newTime);
        if (TotalPlayingTime.TotalMilliseconds > 0)
        {
            CurrentPosition =
                100 * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
        }
        UpdateCurrentLyricIndex(newTime);
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
                _ => 1,
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
                _ => 2,
            };
        }
    }

    public async Task UpdateLyric(string lyric)
    {
        CurrentLyric = await LyricSlice.GetLyricSlices(lyric);
        _currentLyricIndex = 0;
        CurrentLyricContent = "";

        if (CurrentLyric.Count > 0)
        {
            CurrentLyric[0].IsCurrent = true;
            CurrentLyricContent = CurrentLyric[0].Content;
        }
    }

    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);
        await _localSettingsService.SaveSettingAsync("SourceMode", SourceMode);
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
        await _localSettingsService.SaveSettingAsync("CurrentBriefSong", _currentBriefSong);
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        try
        {
            PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>("PlayQueueIndex");
            SourceMode = await _localSettingsService.ReadSettingAsync<byte>("SourceMode");
            ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
            RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
            IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
            _currentBriefSong = SourceMode switch
            {
                0 => await _localSettingsService.ReadSettingAsync<BriefLocalSongInfo>(
                    "CurrentBriefSong"
                ),
                1 => await _localSettingsService.ReadSettingAsync<BriefCloudOnlineSongInfo>(
                    "CurrentBriefSong"
                ),
                _ => null,
            };
            (PlayQueue, ShuffledPlayQueue) = await FileManager.LoadPlayQueueDataAsync();
            _playQueueLength = PlayQueue.Count;
            if (_currentBriefSong is not null)
            {
                CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                    _currentBriefSong
                );
                await SetSource(CurrentSong!.Path);
                _ = UpdateLyric(CurrentSong!.Lyric);
                _systemControls.IsPlayEnabled = true;
                _systemControls.IsPauseEnabled = true;
            }
            if (Data.NotFirstUsed)
            {
                CurrentVolume = await _localSettingsService.ReadSettingAsync<double>(
                    "CurrentVolume"
                );
                PlaySpeed = await _localSettingsService.ReadSettingAsync<double>("PlaySpeed");
            }
            else
            {
                CurrentVolume = 100;
                PlaySpeed = 1;
            }
            Data.RootPlayBarViewModel?.ButtonVisibility =
                CurrentSong is not null && PlayQueue.Any()
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = CurrentSong is not null && PlayQueue.Any();
        }
        catch (Exception ex)
        {
            _currentBriefSong = null;
            CurrentSong = null;
            PlayQueue = [];
            ShuffledPlayQueue = [];
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = false;
            Debug.WriteLine(ex.StackTrace);
        }
    }

    public void Dispose()
    {
        PositionUpdateTimer250ms?.Cancel();
        _currentMedia?.Dispose();
        _vlcPlayer?.Dispose();
        _libVlc?.Dispose();
        _currentCoverStream?.Dispose();
        _windowsMediaPlayer?.Dispose();
    }
}
