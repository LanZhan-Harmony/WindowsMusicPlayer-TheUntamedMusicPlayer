using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Services;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace The_Untamed_Music_Player.Models;

public class MusicPlayer : INotifyPropertyChanged
{
    private readonly ILocalSettingsService _localSettingsService;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private MediaPlayer _player = new()
    {
        AudioCategory = MediaPlayerAudioCategory.Media,
    };
    /// <summary>
    /// 音乐播放器
    /// </summary>
    public MediaPlayer Player
    {
        get => _player;
        set => _player = value;
    }

    /// <summary>
    /// SMTC控件
    /// </summary>
    public SystemMediaTransportControls SystemControls
    {
        get; set;
    }

    /// <summary>
    /// SMTC显示内容更新器
    /// </summary>
    public SystemMediaTransportControlsDisplayUpdater DisplayUpdater
    {
        get; set;
    }

    /// <summary>
    /// 排序方式
    /// </summary>
    private byte _sortMode;
    public byte SortMode
    {
        get => _sortMode;
        set => _sortMode = value;
    }

    private string? _playQueueName;
    /// <summary>
    /// 播放队列名
    /// </summary>
    public string? PlayQueueName
    {
        get => _playQueueName;
        set
        {
            _playQueueName = value;
            OnPropertyChanged(nameof(PlayQueueName));
        }
    }

    private ObservableCollection<BriefMusicInfo> _playQueue = [];
    /// <summary>
    /// 播放队列集合
    /// </summary>
    public ObservableCollection<BriefMusicInfo> PlayQueue
    {
        get => _playQueue;
        set => _playQueue = value;
    }

    private ObservableCollection<BriefMusicInfo> _shuffledPlayQueue = [];
    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    public ObservableCollection<BriefMusicInfo> ShuffledPlayQueue
    {
        get => _shuffledPlayQueue;
        set => _shuffledPlayQueue = value;
    }

    private int _playQueueLength;
    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    public int PlayQueueLength
    {
        get => _playQueueLength;
        set
        {
            _playQueueLength = value;
            OnPropertyChanged(nameof(PlayQueueLength));
        }
    }

    private int _playQueueIndex;
    /// <summary>
    /// 当前歌曲在播放队列中的索引
    /// </summary>
    public int PlayQueueIndex
    {
        get => _playQueueIndex;
        set
        {
            _playQueueIndex = value;
            if (value == 0 && (RepeatMode == 0 || RepeatMode == 2))
            {
                SystemControls.IsPreviousEnabled = false;
            }
            else if (value == PlayQueueLength - 1 && (RepeatMode == 0 || RepeatMode == 2))
            {
                SystemControls.IsNextEnabled = false;
            }
            else
            {
                SystemControls.IsPreviousEnabled = true;
                SystemControls.IsNextEnabled = true;
            }
        }
    }

    private bool _shuffleMode = false;
    /// <summary>
    /// 随机播放模式, true为开启, false为关闭.
    /// </summary>
    public bool ShuffleMode
    {
        get => _shuffleMode;
        set => _shuffleMode = value;
    }

    private byte _repeatMode = 0;
    /// <summary>
    /// 循环播放模式, 0为不循环, 1为列表循环, 2为单曲循环
    /// </summary>
    public byte RepeatMode
    {
        get => _repeatMode;
        set
        {
            _repeatMode = value;
            if (PlayQueueIndex == 0 && (value == 0 || value == 2))
            {
                SystemControls.IsPreviousEnabled = false;
            }
            else if (PlayQueueIndex == PlayQueueLength - 1 && (value == 0 || value == 2))
            {
                SystemControls.IsNextEnabled = false;
            }
            else
            {
                SystemControls.IsPreviousEnabled = true;
                SystemControls.IsNextEnabled = true;
            }
            OnPropertyChanged(nameof(RepeatMode));
        }
    }

    private byte _playState;
    /// <summary>
    /// 播放状态, 0为暂停, 1为播放, 2为加载中
    /// </summary>
    public byte PlayState
    {
        get => _playState;
        set
        {
            _playState = value;
            OnPropertyChanged(nameof(PlayState));
        }
    }

    private DetailedMusicInfo _currentMusic = new();
    /// <summary>
    /// 当前播放歌曲
    /// </summary>
    public DetailedMusicInfo CurrentMusic
    {
        get => _currentMusic;
        set
        {
            _currentMusic = value;

            //给播放器设置音乐源
            SetSource(value.Path);
            CurrentLyric = LyricSlice.GetLyricSlices(value.Lyric);
            OnPropertyChanged(nameof(CurrentMusic));
        }
    }

    private ObservableCollection<LyricSlice> _currentLyric = [];
    /// <summary>
    /// 当前歌词切片集合
    /// </summary>
    public ObservableCollection<LyricSlice> CurrentLyric
    {
        get => _currentLyric;
        set
        {
            _currentLyric = value;
            OnPropertyChanged(nameof(CurrentLyric));
        }
    }

    private int _currentLyricIndex;
    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    public int CurrentLyricIndex
    {
        get => _currentLyricIndex;
        set
        {
            _currentLyricIndex = value;
            OnPropertyChanged(nameof(CurrentLyricIndex));
        }
    }

    /// <summary>
    /// 当前歌词内容
    /// </summary>
    private string _currentLyricContent = "";
    public string CurrentLyricContent
    {
        get => _currentLyricContent;
        set
        {
            _currentLyricContent = value;
            OnPropertyChanged(nameof(CurrentLyricContent));
        }
    }

    /// <summary>
    /// 线程计时器
    /// </summary>
    private ThreadPoolTimer? positionUpdateTimer;

    /// <summary>
    /// 线程锁开启状态, true为开启, false为关闭
    /// </summary>
    private bool lockable = false;


    private TimeSpan _current;
    /// <summary>
    /// 当前播放时间
    /// </summary>
    public TimeSpan Current
    {
        get => _current;
        set
        {
            _current = value;
            OnPropertyChanged(nameof(Current));
        }
    }

    private TimeSpan _total;
    /// <summary>
    /// 当前歌曲总时长
    /// </summary>
    public TimeSpan Total
    {
        get => _total;
        set
        {
            _total = value;
            OnPropertyChanged(nameof(Total));
        }
    }

    private double _currentPosition;
    /// <summary>
    /// 当前播放进度(百分比)
    /// </summary>
    public double CurrentPosition
    {
        get => _currentPosition;
        set
        {
            _currentPosition = value;
            OnPropertyChanged(nameof(CurrentPosition));
        }
    }

    private double _currentVolume = 100;
    /// <summary>
    /// 当前音量
    /// </summary>
    public double CurrentVolume
    {
        get => _currentVolume;
        set
        {
            _currentVolume = value;
            if (!IsMute)
            {
                Player.Volume = value / 100;
            }
            OnPropertyChanged(nameof(CurrentVolume));
        }
    }

    private bool _isMute;
    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    public bool IsMute
    {
        get => _isMute;
        set
        {
            _isMute = value;
            if (value)
            {
                Player.IsMuted = true;
            }
            else
            {
                Player.IsMuted = false;
            }
            OnPropertyChanged(nameof(IsMute));
        }
    }

    private double _playSpeed = 1;
    public double PlaySpeed
    {
        get => _playSpeed;
        set
        {
            _playSpeed = value;
            Player.PlaybackSession.PlaybackRate = value;
        }
    }

    /// <summary>
    /// 线程锁, 用于限制对Player的访问
    /// </summary>
    private readonly Lock mediaLock = new();

    public MusicPlayer()
    {
        _localSettingsService = App.GetService<ILocalSettingsService>();
        Player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        Player.MediaEnded += OnPlaybackStopped;
        Player.MediaFailed += OnPlaybackFailed;
        Player.Volume = CurrentVolume / 100;
        Player.CommandManager.IsEnabled = false;
        SystemControls = Player.SystemMediaTransportControls;
        DisplayUpdater = SystemControls.DisplayUpdater;
        DisplayUpdater.Type = MediaPlaybackType.Music;
        SystemControls.IsEnabled = true;
        SystemControls.ButtonPressed += SystemControls_ButtonPressed;
        LoadCurrentStateAsync();
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="path"></param>
    public void PlaySongByPath(string path)
    {
        lock (mediaLock)
        {
            Stop();
            CurrentMusic = new DetailedMusicInfo(path);
            SystemControls.IsPlayEnabled = true;
            SystemControls.IsPauseEnabled = true;

            var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
            for (var i = 0; i < queue.Count; i++)
            {
                if (queue[i].Path == path)
                {
                    PlayQueueIndex = i;
                    break;
                }
            }

            Play();
        }
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isLast"></param>
    private void PlaySongByIndex(int index, bool isLast = false)
    {
        lock (mediaLock)
        {
            Stop();
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[index] : PlayQueue[index];
            CurrentMusic = new DetailedMusicInfo(songToPlay.Path);
            PlayQueueIndex = isLast ? 0 : index;
            SystemControls.IsPlayEnabled = true;
            SystemControls.IsPauseEnabled = true;
            if (!isLast)
            {
                Play();
            }
        }
    }

    /// <summary>
    /// 为播放器设置音乐源
    /// </summary>
    /// <param name="path"></param>
    private async void SetSource(string path)
    {
        lock (mediaLock)
        {
            try
            {
                Player.Source = null;
                var mediaFileTask = StorageFile.GetFileFromPathAsync(path).AsTask();
                mediaFileTask.Wait();
                var mediaFile = mediaFileTask.Result;
                Player.Source = MediaSource.CreateFromStorageFile(mediaFile);
                Player.PlaybackSession.PlaybackRate = PlaySpeed;
                Total = Player.PlaybackSession.NaturalDuration;
                DisplayUpdater.MusicProperties.Title = CurrentMusic.Title;
                DisplayUpdater.MusicProperties.Artist = CurrentMusic.ArtistsStr == "未知艺术家" ? "" : CurrentMusic.ArtistsStr;
                positionUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer(UpdateTimerHandler, TimeSpan.FromMilliseconds(250), UpdateTimerDestoyed);
            }
            catch { }
        }

        if (CurrentMusic.Cover != null && CurrentMusic.CoverBuffer.Length != 0)
        {
            try
            {
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var coverFileName = "Cover.jpg";
                var coverFile = await tempFolder.CreateFileAsync(coverFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBytesAsync(coverFile, CurrentMusic.CoverBuffer);
                DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromFile(coverFile);
            }
            catch
            {
                DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/NoCover.png"));
            }
        }
        else
        {
            DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/NoCover.png"));
        }
        DisplayUpdater.Update();
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public async void SetPlayList(string name, ObservableCollection<BriefMusicInfo> list, byte sortmode)
    {
        if (PlayQueue.Count != list.Count || PlayQueueName != name || SortMode != sortmode)
        {
            SortMode = sortmode;
            PlayQueueName = name;
            PlayQueue = [.. list];
            PlayQueueLength = list.Count;
            var hasMusics = PlayQueue.Any();
            if (Data.RootPlayBarViewModel != null)
            {
                Data.RootPlayBarViewModel.ButtonVisibility = hasMusics ? Visibility.Visible : Visibility.Collapsed;
                Data.RootPlayBarViewModel.Availability = hasMusics;
            }
            await UpdateShufflePlayQueue();
        }
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler(ThreadPoolTimer timer)
    {
        lock (mediaLock)
        {
            try
            {
                if (Player?.PlaybackSession == null || lockable || Player.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                {
                    return;
                }
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    Current = Player.PlaybackSession.Position;
                    Total = Player.PlaybackSession.NaturalDuration;
                    if (Total.TotalMilliseconds > 0)
                    {
                        CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
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
            catch { }
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
    /// 计时器销毁事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerDestoyed(ThreadPoolTimer timer)
    {
        timer.Cancel();
        positionUpdateTimer?.Cancel();
        positionUpdateTimer = null;
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
            switch (Player?.PlaybackSession.PlaybackState)
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
                        SystemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    });
                    break;
                case MediaPlaybackState.Paused:
                    Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 0;
                        SystemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    });
                    break;
                default:
                    break;
            }
        }
        catch { }
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
            if (Player?.PlaybackSession.PlaybackState == MediaPlaybackState.Paused && !lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByPath(CurrentMusic.Path);
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
            if (RepeatMode == 2)
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
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    PlayPreviousSong();
                });
                break;
            case SystemMediaTransportControlsButton.Next:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    PlayNextSong();
                });
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
    private void Play()
    {
        Player?.Play();
    }

    /// <summary>
    /// 暂停
    /// </summary>
    private void Pause()
    {
        Player?.Pause();
    }

    /// <summary>
    /// 停止
    /// </summary>
    private void Stop()
    {
        Player?.Pause();
        Current = TimeSpan.Zero;
        CurrentPosition = 0;
        CurrentLyricContent = "";
        positionUpdateTimer?.Cancel();
        positionUpdateTimer = null;
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
                newIndex = (PlayQueueIndex + PlayQueueLength - 1) % PlayQueueLength;
            }

            PlaySongByIndex(newIndex);
        }
        catch { }
    }

    /// <summary>
    /// 播放下一曲
    /// </summary>
    public void PlayNextSong()
    {
        try
        {
            var newIndex = PlayQueueIndex < PlayQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var isLast = PlayQueueIndex >= PlayQueueLength - 1;

            if (RepeatMode == 1) // 列表循环
            {
                newIndex = (PlayQueueIndex + 1) % PlayQueueLength;
                isLast = false;
            }

            PlaySongByIndex(newIndex, isLast);
        }
        catch { }
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
    public async void ShuffleModeUpdate()
    {
        ShuffleMode = !ShuffleMode;
        if (ShuffleMode)
        {
            await UpdateShufflePlayQueue();
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                if (PlayQueue[i].Path == CurrentMusic.Path)
                {
                    PlayQueueIndex = i;
                    break;
                }
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
    public async Task UpdateShufflePlayQueue()
    {
        await Task.Run(() =>
        {
            ShuffledPlayQueue = new ObservableCollection<BriefMusicInfo>([.. PlayQueue.OrderBy(x => Guid.NewGuid())]);
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                if (ShuffledPlayQueue[i].Path == CurrentMusic.Path)
                {
                    PlayQueueIndex = i;
                    break;
                }
            }
        });
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        SystemControls.IsPlayEnabled = false;
        SystemControls.IsPauseEnabled = false;
        SystemControls.IsPreviousEnabled = false;
        SystemControls.IsNextEnabled = false;
        Total = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        CurrentLyric.Clear();
        PlayQueueName = "";
        PlayQueueIndex = 0;
        PlayQueueLength = 0;
        if (Data.RootPlayBarViewModel != null)
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
        lockable = true;
        Current = TimeSpan.FromMilliseconds((double)((Slider)sender).Value * Total.TotalMilliseconds / 100);
    }

    /// <summary>
    /// 滑动滑动条事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void SliderUpdate(object sender, PointerRoutedEventArgs e)
    {
        Current = TimeSpan.FromMilliseconds((double)((Slider)sender).Value * Total.TotalMilliseconds / 100);
        CurrentLyricIndex = GetCurrentLyricIndex(Current.TotalMilliseconds);
    }

    /// <summary>
    /// 松开滑动条更新播放进度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressUpdate(object sender, PointerRoutedEventArgs e)
    {
        Player.PlaybackSession.Position = TimeSpan.FromMilliseconds((double)((Slider)sender).Value * Total.TotalMilliseconds / 100);
        Current = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        Total = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (Total.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        lockable = false;
    }

    /// <summary>
    /// 点击歌词更新播放进度
    /// </summary>
    /// <param name="time"></param>
    public void LyricProgressUpdate(double time)
    {
        lockable = true;
        Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(time);
        Current = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        Total = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (Total.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        lockable = false;
    }

    public void SkipBack10sButton_Click(object sender, RoutedEventArgs e)
    {
        lockable = true;
        if (Player.PlaybackSession.Position.TotalMilliseconds - 10000 < 0)
        {
            Player.PlaybackSession.Position = TimeSpan.Zero;
        }
        else
        {
            Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(Player.PlaybackSession.Position.TotalMilliseconds - 10000);
        }
        Current = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        Total = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (Total.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        lockable = false;
    }

    public void SkipForw30sButton_Click(object sender, RoutedEventArgs e)
    {
        lockable = true;
        if (Player.PlaybackSession.Position.TotalMilliseconds + 30000 > Total.TotalMilliseconds)
        {
            Player.PlaybackSession.Position = Total;
        }
        else
        {
            Player.PlaybackSession.Position = TimeSpan.FromMilliseconds(Player.PlaybackSession.Position.TotalMilliseconds + 30000);
        }
        Current = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
        Total = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
        if (Total.TotalMilliseconds > 0)
        {
            CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
        }
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        lockable = false;
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

    /// <summary>
    /// 获取歌词字体大小
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public double GetLyricFont(double itemTime, int currentLyricIndex)
    {
        double defaultFontSize = Data.MainWindow?.Width <= 1000 ? 16 : 20;
        double highlightedFontSize = Data.MainWindow?.Width <= 1000 ? 24 : 50;

        try
        {
            return itemTime == CurrentLyric[currentLyricIndex].Time ? highlightedFontSize : defaultFontSize;
        }
        catch
        {
            return defaultFontSize;
        }
    }

    /// <summary>
    /// 获取歌词边距
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public Thickness GetLyricMargin(double itemTime, int currentLyricIndex)
    {
        var defaultMargin = new Thickness(0, 20, 0, 20);
        var highlightedMargin = new Thickness(0, 40, 0, 40);

        try
        {
            return itemTime == CurrentLyric[currentLyricIndex].Time ? highlightedMargin : defaultMargin;
        }
        catch
        {
            return defaultMargin;
        }
    }

    /// <summary>
    /// 获取歌词透明度 
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="currentLyricIndex"></param>
    /// <returns></returns>
    public double GetLyricOpacity(double itemTime, int currentLyricIndex)
    {
        const double defaultOpacity = 0.5;
        const double highlightedOpacity = 1.0;

        try
        {
            return itemTime == CurrentLyric[currentLyricIndex].Time ? highlightedOpacity : defaultOpacity;
        }
        catch
        {
            return defaultOpacity;
        }
    }

    /// <summary>
    /// 获取播放队列
    /// </summary>
    /// <param name="PlayQueueName"></param>
    /// <param name="ShuffleMode"></param>
    /// <returns></returns>
    public ObservableCollection<BriefMusicInfo> GetPlayQueue(string PlayQueueName, bool ShuffleMode) => ShuffleMode ? ShuffledPlayQueue : PlayQueue;


    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        await _localSettingsService.SaveSettingAsync("CurrentMusic", CurrentMusic.Path);
        /*var playqueuepaths = PlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("PlayQueuePaths", playqueuepaths);
        var shuffledplayqueuepaths = ShuffledPlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("ShuffledPlayQueuePaths", shuffledplayqueuepaths);
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);*/
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        var currentMusicPath = await _localSettingsService.ReadSettingAsync<string>("CurrentMusic");
        if (!string.IsNullOrEmpty(currentMusicPath))
        {
            CurrentMusic = new DetailedMusicInfo(currentMusicPath);
        }
        /*var playqueuepaths = await _localSettingsService.ReadSettingAsync<List<string>>("PlayQueuePaths");
        if (playqueuepaths != null)
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
        if (shuffledplayqueuepaths != null)
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
        ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
        RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
        IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
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
        /*if (Data.RootPlayBarViewModel != null)
        {
            Data.RootPlayBarViewModel.ButtonVisibility = PlayQueue.Any() ? Visibility.Visible : Visibility.Collapsed;
            Data.RootPlayBarViewModel.Availability = PlayQueue.Any();
        }*/
    }
}