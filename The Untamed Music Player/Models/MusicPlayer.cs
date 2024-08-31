using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Views;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System.Threading;

namespace The_Untamed_Music_Player.Models;

public partial class MusicPlayer : INotifyPropertyChanged
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
        set => _playQueueIndex = value;
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

    private int _repeatMode = 0;
    /// <summary>
    /// 循环播放模式, 0为不循环, 1为列表循环, 2为单曲循环
    /// </summary>
    public int RepeatMode
    {
        get => _repeatMode;
        set
        {
            _repeatMode = value;
            OnPropertyChanged(nameof(RepeatMode));
        }
    }

    private int _playState;
    /// <summary>
    /// 播放状态, 0为暂停, 1为播放, 2为加载中
    /// </summary>
    public int PlayState
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
    /// 线程计时器
    /// </summary>
    private ThreadPoolTimer? positionUpdateTimer;

    /// <summary>
    /// 线程锁开启状态, true为开启, false为关闭
    /// </summary>
    private bool lockable = false;

    /// <summary>
    /// 播放栏UI
    /// </summary>
    public static RootPlayBarView? PlayBarUI
    {
        get; set;
    }

    /// <summary>
    /// 歌词页UI
    /// </summary>
    public static 歌词Page? 歌词UI
    {
        get; set;
    }

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
            Player.Volume = CurrentVolume / 100;
            OnPropertyChanged(nameof(CurrentVolume));
        }
    }

    /// <summary>
    /// 线程锁, 用于限制对Player的访问
    /// </summary>
    private readonly Lock mediaLock = new();

    public MusicPlayer(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        LoadCurrentStateAsync();
        Player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        Player.MediaEnded += OnPlaybackStopped;
        Player.Volume = CurrentVolume / 100;
        Player.CommandManager.IsEnabled = true;
    }

    ~MusicPlayer()
    {
        Player.Dispose();
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="path"></param>
    /// <param name="isLast">是否是播放列表中最后一曲</param>
    public void PlaySongByPath(string path, bool isLast = false)
    {
        lock (mediaLock)
        {
            if (!isLast)
            {
                Stop();
                CurrentMusic = new DetailedMusicInfo(path);
                if (ShuffleMode)
                {
                    PlayQueueIndex = ShuffledPlayQueue.ToList().FindIndex(x => x.Path == path);
                }
                else
                {
                    PlayQueueIndex = PlayQueue.ToList().FindIndex(x => x.Path == path);
                }
                Play();
            }
            else
            {
                Stop();
                CurrentMusic = new DetailedMusicInfo(path);
                PlayQueueIndex = PlayQueue.ToList().FindIndex(x => x.Path == path);
            }
        }
    }

    /// <summary>
    /// 为播放器设置音乐源
    /// </summary>
    /// <param name="path"></param>
    private void SetSource(string path)
    {
        lock (mediaLock)
        {
            try
            {
                var mediaFileTask = StorageFile.GetFileFromPathAsync(path).AsTask();
                mediaFileTask.Wait();
                var mediaFile = mediaFileTask.Result;
                Player.Source = MediaSource.CreateFromStorageFile(mediaFile);
                Total = Player.PlaybackSession.NaturalDuration;
                positionUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer(UpdateTimerHandler, TimeSpan.FromMilliseconds(250), UpdateTimerDestoyed);
            }
            catch { }
        }
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public async void SetPlayList(string name, ObservableCollection<BriefMusicInfo> list)
    {
        PlayQueueName = name;
        PlayQueue = new ObservableCollection<BriefMusicInfo>(list);
        PlayQueueLength = list.Count;
        if (Data.RootPlayBarViewModel != null)
        {
            Data.RootPlayBarViewModel.ButtonVisibility = PlayQueue.Any() ? Visibility.Visible : Visibility.Collapsed;
            Data.RootPlayBarViewModel.Availability = PlayQueue.Any();
        }
        await UpdateShufflePlayQueue();
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler(ThreadPoolTimer timer)
    {
        lock (mediaLock)
        {
            if (Player != null && !lockable && Player.PlaybackSession.PlaybackState != MediaPlaybackState.None && Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                try
                {
                    PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {

                        Current = Player.PlaybackSession?.Position ?? TimeSpan.Zero;
                        Total = Player.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;
                        CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
                    });
                }
                catch { }
                try
                {
                    歌词UI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
                    });
                }
                catch { }
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
        var index = CurrentLyric.ToList().FindIndex(x => x.Time > currentTime);
        if (index > 0)
        {
            index--;
        }
        if (index < 0)
        {
            index = CurrentLyric.Count - 1;
        }
        return index;
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
                    PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 2;
                    });
                    break;
                case MediaPlaybackState.Buffering:
                    PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 2;
                    });
                    break;
                case MediaPlaybackState.Playing:
                    PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 1;
                    });
                    break;
                case MediaPlaybackState.Paused:
                    PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        PlayState = 0;
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
        PlayBarUI?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
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
            switch (ShuffleMode, RepeatMode)
            {
                case (false, 0):
                    {
                        if (PlayQueueIndex > 0)
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex - 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex].Path);
                        }
                        break;
                    }
                case (false, 1):
                    {
                        {
                            PlaySongByPath(PlayQueue[(PlayQueueIndex + PlayQueueLength - 1) % PlayQueueLength].Path);
                            break;
                        }
                    }
                case (false, 2):
                    {
                        if (PlayQueueIndex > 0)
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex - 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex].Path);
                        }
                        break;
                    }
                case (true, 0):
                    {
                        if (PlayQueueIndex > 0)
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex - 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex].Path);
                        }
                        break;
                    }
                case (true, 1):
                    {
                        {
                            PlaySongByPath(ShuffledPlayQueue[(PlayQueueIndex + PlayQueueLength - 1) % PlayQueueLength].Path);
                            break;
                        }
                    }
                case (true, 2):
                    {
                        if (PlayQueueIndex > 0)
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex - 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex].Path);
                        }
                        break;
                    }
                default:
                    break;
            }
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
            switch (ShuffleMode, RepeatMode)
            {
                case (false, 0):
                    {
                        if (PlayQueueIndex < PlayQueueLength - 1)
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex + 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(PlayQueue[0].Path, true);
                        }
                        break;
                    }
                case (false, 1):
                    {
                        {
                            PlaySongByPath(PlayQueue[(PlayQueueIndex + 1) % PlayQueueLength].Path);
                            break;
                        }
                    }
                case (false, 2):
                    {
                        if (PlayQueueIndex < PlayQueueLength - 1)
                        {
                            PlaySongByPath(PlayQueue[PlayQueueIndex + 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(PlayQueue[0].Path, true);
                        }
                        break;
                    }
                case (true, 0):
                    {
                        if (PlayQueueIndex < PlayQueueLength - 1)
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex + 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(ShuffledPlayQueue[0].Path, true);
                        }
                        break;
                    }
                case (true, 1):
                    {
                        {
                            PlaySongByPath(ShuffledPlayQueue[(PlayQueueIndex + 1) % PlayQueueLength].Path);
                            break;
                        }
                    }
                case (true, 2):
                    {
                        if (PlayQueueIndex < PlayQueueLength - 1)
                        {
                            PlaySongByPath(ShuffledPlayQueue[PlayQueueIndex + 1].Path);
                        }
                        else
                        {
                            PlaySongByPath(ShuffledPlayQueue[0].Path, true);
                        }
                        break;
                    }
                default:
                    break;
            }
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
            PlayQueueIndex = PlayQueue.ToList().FindIndex(x => x.Path == CurrentMusic.Path);
        }
        OnPropertyChanged(nameof(ShuffleMode));
    }

    /// <summary>
    /// 循环播放模式更新
    /// </summary>
    public void RepeatModeUpdate()
    {
        RepeatMode = (RepeatMode + 1) % 3;
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
            PlayQueueIndex = ShuffledPlayQueue.ToList().FindIndex(x => x.Path == CurrentMusic.Path);
        });
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
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
        CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
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
        CurrentPosition = 100 * (Current.TotalMilliseconds / Total.TotalMilliseconds);
        CurrentLyricIndex = GetCurrentLyricIndex((Player.PlaybackSession?.Position ?? TimeSpan.Zero).TotalMilliseconds);
        lockable = false;
    }

    /// <summary>
    /// 获取歌词字体大小
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="CurrentLyricIdx"></param>
    /// <returns></returns>
    public double GetLyricFont(double itemTime, int CurrentLyricIdx)
    {
        try
        {
            if (Data.MainWindow?.Width <= 1000)
            {
                if (itemTime == CurrentLyric[CurrentLyricIdx].Time)
                {
                    return 24;
                }
                else
                {
                    return 16;
                }
            }
            else
            {
                if (itemTime == CurrentLyric[CurrentLyricIdx].Time)
                {
                    return 50;
                }
                else
                {
                    return 20;
                }
            }
        }
        catch
        {
            return 20;
        }
    }

    /// <summary>
    /// 获取歌词边距
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="CurrentLyricIdx"></param>
    /// <returns></returns>
    public Thickness GetLyricMargin(double itemTime, int CurrentLyricIdx)
    {
        try
        {
            if (itemTime == CurrentLyric[CurrentLyricIdx].Time)
            {
                return new Thickness(0, 40, 0, 40);
            }
            else
            {
                return new Thickness(0, 20, 0, 20);
            }
        }
        catch
        {
            return new Thickness(0, 20, 0, 20);
        }
    }

    /// <summary>
    /// 获取歌词透明度 
    /// </summary>
    /// <param name="itemTime"></param>
    /// <param name="CurrentLyricIdx"></param>
    /// <returns></returns>
    public double GetLyricOpacity(double itemTime, int CurrentLyricIdx)
    {
        try
        {
            if (itemTime == CurrentLyric[CurrentLyricIdx].Time)
            {
                return 1;
            }
            else
            {
                return 0.5;
            }
        }
        catch
        {
            return 0.5;
        }
    }

    /// <summary>
    /// 获取播放队列
    /// </summary>
    /// <param name="PlayQueueName"></param>
    /// <param name="ShuffleMode"></param>
    /// <returns></returns>
    public ObservableCollection<BriefMusicInfo> GetPlayQueue(string PlayQueueName, bool ShuffleMode)
    {
        if (ShuffleMode)
        {
            return ShuffledPlayQueue;
        }
        else
        {
            return PlayQueue;
        }
    }

    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async void SaveCurrentStateAsync()
    {
        await _localSettingsService.SaveSettingAsync("NotFirstUsed", true);
        await _localSettingsService.SaveSettingAsync("CurrentMusic", CurrentMusic.Path);
        /*var playqueuepaths = PlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("PlayQueuePaths", playqueuepaths);
        var shuffledplayqueuepaths = ShuffledPlayQueue.Select(music => music.Path).ToList();
        await _localSettingsService.SaveSettingAsync("ShuffledPlayQueuePaths", shuffledplayqueuepaths);
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);*/
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        var notFirstUsed = await _localSettingsService.ReadSettingAsync<bool>("NotFirstUsed");
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
        RepeatMode = await _localSettingsService.ReadSettingAsync<int>("RepeatMode");
        if (notFirstUsed)
        {
            CurrentVolume = await _localSettingsService.ReadSettingAsync<double>("CurrentVolume");
        }
        else
        {
            CurrentVolume = 100;
        }
        /*if (Data.RootPlayBarViewModel != null)
        {
            Data.RootPlayBarViewModel.ButtonVisibility = PlayQueue.Any() ? Visibility.Visible : Visibility.Collapsed;
            Data.RootPlayBarViewModel.Availability = PlayQueue.Any();
        }*/
    }
}