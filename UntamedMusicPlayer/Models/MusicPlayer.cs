using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Wasapi;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Services;
using Windows.Media;
using Windows.System;
using Windows.System.Threading;
using ZLinq;
using ZLogger;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace UntamedMusicPlayer.Models;

public partial class MusicPlayer
    : ObservableRecipient,
        IRecipient<FontSizeChangeMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private static readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();

    /// <summary>
    /// SMTC管理器
    /// </summary>
    private readonly SystemMediaTransportControlsManager _smtcManager = new();

    /// <summary>
    /// 线程锁开启状态, true为开启, false为关闭
    /// </summary>
    private bool _lockable = false;

    /// <summary>
    /// 播放失败次数
    /// </summary>
    private byte _failedCount = 0;

    /// <summary>
    /// 播放队列歌曲数量
    /// </summary>
    private int _playQueueLength = 0;

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    private int _currentLyricIndex = 0;

    /// <summary>
    /// Bass音频流句柄
    /// </summary>
    private int _currentStream = 0;

    /// <summary>
    /// 用于变速不变调的Tempo流句柄
    /// </summary>
    private int _tempoStream = 0;

    /// <summary>
    /// WASAPI输出设备编号
    /// </summary>
    private int _wasapiDevice = -1;

    /// <summary>
    /// 播放结束事件回调
    /// </summary>
    private SyncProcedure? _syncEndCallback;

    /// <summary>
    /// 播放失败事件回调
    /// </summary>
    private SyncProcedure? _syncFailCallback;

    /// <summary>
    /// WASAPI回调处理程序
    /// </summary>
    private WasapiProcedure? _wasapiProc;

    /// <summary>
    /// 是否已经加载完成
    /// </summary>
    public bool HasLoaded { get; private set; } = false;

    /// <summary>
    /// 播放速度
    /// </summary>
    public double PlaySpeed
    {
        get;
        set
        {
            field = value;
            SetPlaybackSpeed(value);
        }
    } = 1.0;

    /// <summary>
    /// 线程计时器
    /// </summary>
    public ThreadPoolTimer? PositionUpdateTimer250ms { get; set; }

    /// <summary>
    /// 播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> PlayQueue { get; set; } = [];

    /// <summary>
    /// 随机播放队列集合
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> ShuffledPlayQueue { get; set; } = [];

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
        _smtcManager.UpdatePlayQueueInfo(value, _playQueueLength, RepeatMode);
    }

    /// <summary>
    /// 循环播放模式, 0为不循环, 1为列表循环, 2为单曲循环
    /// </summary>
    [ObservableProperty]
    public partial byte RepeatMode { get; set; } = 0;

    partial void OnRepeatModeChanged(byte value)
    {
        _smtcManager.UpdatePlayQueueInfo(PlayQueueIndex, _playQueueLength, value);
    }

    /// <summary>
    /// 播放状态, 0为暂停, 1为播放, 2为加载中
    /// </summary>
    [ObservableProperty]
    public partial byte PlayState { get; set; } = 0;

    /// <summary>
    /// 当前播放的歌曲简要版
    /// </summary>
    [ObservableProperty]
    public partial IBriefSongInfoBase? CurrentBriefSong { get; set; }

    partial void OnCurrentBriefSongChanged(IBriefSongInfoBase? value)
    {
        _ = SaveCurrentBriefSongAsync();
    }

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
            SetVolumeValue(value / 100.0);
        }
    }

    /// <summary>
    /// 是否静音, true为静音, false为非静音
    /// </summary>
    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    partial void OnIsMuteChanged(bool value)
    {
        SetVolumeValue(value ? 0.0 : CurrentVolume / 100.0);
    }

    /// <summary>
    /// 是否启用WASAPI独占模式
    /// </summary>
    public bool IsExclusiveMode { get; set; } = false;

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
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        InitializeBass();
        InitializeSmtc();
        LoadCurrentStateAsync();
    }

    public void Receive(FontSizeChangeMessage message)
    {
        foreach (var slice in CurrentLyric)
        {
            slice.UpdateStyle();
        }
    }

    /// <summary>
    /// 初始化Bass音频库
    /// </summary>
    private void InitializeBass()
    {
        if (!Bass.Init()) // 初始化Bass - 使用默认设备
        {
            _logger.ZLogInformation($"Bass初始化失败: {Bass.LastError}");
            return;
        }

        LoadBassPlugins(); // 加载Bass插件

        // 设置同步回调
        _syncEndCallback = OnPlayBackEnded;
        _syncFailCallback = OnPlaybackFailed;

        // 设置WASAPI回调
        _wasapiProc = WasapiProc;
    }

    /// <summary>
    /// 初始化SMTC
    /// </summary>
    private void InitializeSmtc()
    {
        _smtcManager.ButtonPressed += OnSmtcButtonPressed;
    }

    /// <summary>
    /// 处理SMTC按钮按下事件
    /// </summary>
    private void OnSmtcButtonPressed(SystemMediaTransportControlsButton button)
    {
        switch (button)
        {
            case SystemMediaTransportControlsButton.Play:
            case SystemMediaTransportControlsButton.Pause:
                Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    PlayPauseUpdate
                );
                break;
            case SystemMediaTransportControlsButton.Previous:
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
    /// 加载Bass插件
    /// </summary>
    private static void LoadBassPlugins()
    {
        var appPath = AppContext.BaseDirectory;
        var pluginPaths = new[]
        {
            "bassape.dll",
            "basscd.dll",
            "bassdsd.dll",
            "bassflac.dll",
            "basshls.dll",
            "bassmidi.dll",
            "bassopus.dll",
            "basswebm.dll",
            "basswv.dll",
        };

        foreach (var pluginPath in pluginPaths)
        {
            var fullPath = Path.Combine(appPath, pluginPath);
            Bass.PluginLoad(fullPath);
        }
    }

    /// <summary>
    /// Bass同步回调 - 播放结束
    /// </summary>
    private void OnPlayBackEnded(int handle, int channel, int data, nint user)
    {
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_lockable)
            {
                if (RepeatMode == 2)
                {
                    PlaySongByInfo(CurrentBriefSong!);
                }
                else
                {
                    PlayNextSong();
                }
            }
        });
    }

    /// <summary>
    /// Bass同步回调 - 播放失败
    /// </summary>
    private void OnPlaybackFailed(int handle, int channel, int data, nint user)
    {
        _logger.SongPlaybackError(CurrentSong!.Title);
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || CurrentSong.IsOnline)
            {
                Stop();
            }
            else
            {
                CurrentBriefSong!.IsPlayAvailable = false;
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

    /// <summary>
    /// 处理歌曲不可用的情况
    /// </summary>
    private void HandleSongNotAvailable()
    {
        _logger.SongPlaybackError(CurrentSong!.Title);
        if (PlayQueue.Count == 0)
        {
            return;
        }
        Data.RootPlayBarView?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RepeatMode == 2 || CurrentSong.IsOnline)
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

    /// <summary>
    /// WASAPI回调处理程序
    /// </summary>
    private int WasapiProc(nint buffer, int length, nint user)
    {
        if (_tempoStream == 0)
        {
            return 0;
        }
        var bytesRead = Bass.ChannelGetData(_tempoStream, buffer, length);
        if (bytesRead == -1 || bytesRead == 0)
        {
            return 0;
        }
        return bytesRead;
    }

    /// <summary>
    /// 清理WASAPI资源
    /// </summary>
    private static void CleanupWasapi()
    {
        try
        {
            if (BassWasapi.IsStarted)
            {
                BassWasapi.Stop(true);
            }
            BassWasapi.Free();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"清理WASAPI资源时出错");
        }
    }

    /// <summary>
    /// 初始化WASAPI独占模式
    /// </summary>
    private bool InitializeWasapiExclusive()
    {
        try
        {
            if (_tempoStream == 0)
            {
                _logger.ZLogInformation($"没有有效的音频流，无法初始化WASAPI独占模式");
                return false;
            }

            var result = BassWasapi.Init(
                -1,
                0,
                0,
                WasapiInitFlags.Exclusive | WasapiInitFlags.EventDriven,
                0.05f,
                0,
                _wasapiProc,
                nint.Zero
            );

            if (!result)
            {
                var error = Bass.LastError;
                _logger.ZLogInformation($"WASAPI独占模式初始化失败: {error}");

                if (error == Errors.Already)
                {
                    CleanupWasapi();
                    result = BassWasapi.Init(
                        -1,
                        0,
                        0,
                        WasapiInitFlags.Exclusive | WasapiInitFlags.EventDriven,
                        0.05f,
                        0,
                        _wasapiProc,
                        nint.Zero
                    );

                    if (!result)
                    {
                        _logger.ZLogInformation($"WASAPI独占模式重试初始化失败: {Bass.LastError}");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"初始化WASAPI独占模式时出现异常");
            return false;
        }
    }

    /// <summary>
    /// 启动WASAPI独占模式播放
    /// </summary>
    private bool StartWasapiExclusivePlayback()
    {
        try
        {
            if (!InitializeWasapiExclusive())
            {
                return false;
            }

            if (!BassWasapi.Start())
            {
                var error = Bass.LastError;
                _logger.ZLogInformation($"WASAPI启动失败: {error}");
                CleanupWasapi();
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"启动WASAPI独占模式播放时出现异常");
            CleanupWasapi();
            return false;
        }
    }

    /// <summary>
    /// 停止WASAPI播放
    /// </summary>
    private void StopWasapiPlayback()
    {
        try
        {
            if (BassWasapi.IsStarted)
            {
                BassWasapi.Stop(true);
            }
            if (_wasapiDevice != -1)
            {
                BassWasapi.Free();
                _wasapiDevice = -1;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"停止WASAPI播放时出错");
        }
    }

    /// <summary>
    /// 切换独占模式
    /// </summary>
    public bool SetExclusiveMode(bool isExclusiveMode)
    {
        if (IsExclusiveMode == isExclusiveMode)
        {
            return true;
        }
        var wasPlaying = PlayState == 1;
        var currentTime = CurrentPlayingTime.TotalSeconds;
        var prevMode = IsExclusiveMode;
        try
        {
            if (wasPlaying)
            {
                InternalStop();
            }
            CleanupWasapi();
            IsExclusiveMode = isExclusiveMode;
            if (CurrentSong is not null)
            {
                CreateStreamFromPath(CurrentSong.Path);
                SetVolumeValue(IsMute ? 0.0 : CurrentVolume / 100.0);
                if (wasPlaying)
                {
                    InternalPlay();
                }
                // 在播放启动后设置播放位置，确保在独占模式下位置设置生效
                if (currentTime > 0)
                {
                    SetPlaybackPositionInternal(currentTime);
                }
            }
            return true;
        }
        catch
        {
            IsExclusiveMode = prevMode;
            CleanupWasapi();

            if (CurrentSong is not null)
            {
                try
                {
                    CreateStreamFromPath(CurrentSong.Path);
                    SetVolumeValue(IsMute ? 0.0 : CurrentVolume / 100.0);
                    if (currentTime > 0)
                    {
                        SetPlaybackPositionInternal(currentTime);
                    }
                    if (wasPlaying)
                    {
                        InternalPlay();
                    }
                }
                catch (Exception restoreEx)
                {
                    _logger.ZLogInformation(restoreEx, $"恢复播放失败");
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 计时器更新事件
    /// </summary>
    /// <param name="timer"></param>
    private void UpdateTimerHandler250ms(ThreadPoolTimer timer)
    {
        try
        {
            if (_tempoStream == 0 || _lockable || PlayState != 1)
            {
                return;
            }

            var positionBytes = Bass.ChannelGetPosition(_tempoStream);
            var positionSeconds = Bass.ChannelBytes2Seconds(_tempoStream, positionBytes);
            var playingTime = TimeSpan.FromSeconds(positionSeconds);

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                CurrentPlayingTime = playingTime;
                if (TotalPlayingTime.TotalMilliseconds > 0)
                {
                    CurrentPosition =
                        100 * (playingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
                }
            });

            var dispatcherQueue =
                Data.LyricPage?.DispatcherQueue ?? Data.DesktopLyricWindow?.DispatcherQueue;
            if (CurrentLyric.Count > 0)
            {
                dispatcherQueue?.TryEnqueue(() => UpdateCurrentLyricIndex(positionSeconds * 1000));
            }

            _smtcManager.UpdateTimelinePosition(CurrentPlayingTime);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"计时器更新失败");
        }
    }

    public void NotifyLyricContentChanged()
    {
        OnPropertyChanged(nameof(CurrentLyricContent));
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        PositionUpdateTimer250ms = ThreadPoolTimer.CreatePeriodicTimer(
            UpdateTimerHandler250ms,
            TimeSpan.FromMilliseconds(250)
        );
        InternalPlay();
        PlayState = 1;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Playing);
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        InternalPause();
        PlayState = 0;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Paused);
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer250ms = null;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        InternalStop();
        PlayState = 0;
        CurrentPlayingTime = TimeSpan.Zero;
        CurrentPosition = 0;
        _currentLyricIndex = 0;
        CurrentLyricContent = "";
        PositionUpdateTimer250ms?.Cancel();
        PositionUpdateTimer250ms = null;
    }

    /// <summary>
    /// 内部播放实现
    /// </summary>
    private void InternalPlay()
    {
        if (_tempoStream == 0)
        {
            return;
        }
        if (IsExclusiveMode)
        {
            if (!StartWasapiExclusivePlayback())
            {
                _logger.ZLogInformation($"WASAPI独占模式失败，回退到共享模式");
                IsExclusiveMode = false;

                if (CurrentSong is not null)
                {
                    CreateStreamFromPath(CurrentSong.Path);
                    SetVolumeValue(IsMute ? 0.0 : CurrentVolume / 100.0);
                }
                Bass.ChannelPlay(_tempoStream, false);
            }
        }
        else
        {
            if (!Bass.ChannelPlay(_tempoStream, false))
            {
                var error = Bass.LastError;
                _logger.ZLogInformation($"共享模式播放失败: {error}");
                if (error == Errors.Start)
                {
                    if (Bass.Start())
                    {
                        Bass.ChannelPlay(_tempoStream, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 内部暂停实现
    /// </summary>
    private void InternalPause()
    {
        if (_tempoStream == 0)
        {
            return;
        }
        if (IsExclusiveMode)
        {
            if (BassWasapi.IsStarted)
            {
                BassWasapi.Stop(false);
            }
        }
        else
        {
            Bass.ChannelPause(_tempoStream);
        }
    }

    /// <summary>
    /// 内部停止实现
    /// </summary>
    private void InternalStop()
    {
        if (_tempoStream != 0)
        {
            if (IsExclusiveMode)
            {
                CleanupWasapi();
            }
            Bass.ChannelStop(_tempoStream);
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
            CleanupWasapi();
            CreateStreamFromPath(path);
            SetVolumeValue(IsMute ? 0.0 : CurrentVolume / 100.0);
            _smtcManager.UpdateMediaInfo(
                CurrentSong!.Title,
                CurrentSong.ArtistsStr,
                TotalPlayingTime
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"SetSource失败");
        }

        await _smtcManager.SetCoverImageAsync(CurrentSong!);
        _smtcManager.Update();
    }

    /// <summary>
    /// 按路径播放歌曲
    /// </summary>
    /// <param name="info"></param>
    public async void PlaySongByInfo(IBriefSongInfoBase info)
    {
        Stop();
        PlayState = 2;
        CurrentBriefSong = info;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        PlayQueueIndex =
            queue.AsValueEnumerable().FirstOrDefault(song => song.Song == info)?.Index ?? 0;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _smtcManager.SetButtonsEnabled(true, true, true, true);
            Play();
        }
        else
        {
            HandleSongNotAvailable();
        }
    }

    public void PlaySongByIndexedInfo(IndexedPlayQueueSong info)
    {
        PlaySongByIndex(info.Index);
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isLast"></param>
    private async void PlaySongByIndex(int index, bool isLast = false)
    {
        Stop();
        PlayState = 2;
        var songToPlay = ShuffleMode ? ShuffledPlayQueue[index] : PlayQueue[index];
        CurrentBriefSong = songToPlay.Song;
        CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay.Song);
        PlayQueueIndex = isLast ? 0 : index;
        if (CurrentSong!.IsPlayAvailable)
        {
            await SetSource(CurrentSong!.Path);
            _ = UpdateLyric(CurrentSong!.Lyric);
            _smtcManager.SetButtonsEnabled(true, true, true, true);
            if (isLast)
            {
                PlayState = 0;
            }
            else
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
        queue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, info));
        _playQueueLength++;
        for (var i = insertIndex + 1; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        if (ShuffleMode)
        {
            PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, info));
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
            queue.Insert(insertIndex, new IndexedPlayQueueSong(insertIndex, song));
            insertIndex++;
        }
        _playQueueLength += songs.AsValueEnumerable().Count();
        for (var i = insertIndex; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        if (ShuffleMode)
        {
            foreach (var song in songs)
            {
                PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 将歌曲添加到播放队列
    /// </summary>
    /// <param name="info"></param>
    public void AddSongToPlayQueue(IBriefSongInfoBase info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        queue.Add(new IndexedPlayQueueSong(queue.Count, info));
        _playQueueLength++;
        if (ShuffleMode)
        {
            PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, info));
        }
    }

    /// <summary>
    /// 将歌曲列表添加到播放队列
    /// </summary>
    /// <param name="songs"></param>
    public void AddSongsToPlayQueue(IEnumerable<IBriefSongInfoBase> songs)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        foreach (var song in songs)
        {
            queue.Add(new IndexedPlayQueueSong(queue.Count, song));
        }
        _playQueueLength += songs.AsValueEnumerable().Count();
        if (ShuffleMode)
        {
            foreach (var song in songs)
            {
                PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 从播放队列中移除歌曲
    /// </summary>
    /// <param name="info"></param>
    public async Task RemoveSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        int newIndex;
        if (index == PlayQueueIndex)
        {
            var playState = PlayState;
            Stop();
            newIndex = PlayQueueIndex < _playQueueLength - 1 ? PlayQueueIndex + 1 : 0;
            var songToPlay = ShuffleMode ? ShuffledPlayQueue[newIndex] : PlayQueue[newIndex];
            CurrentBriefSong = songToPlay.Song;
            CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(songToPlay.Song);
            PlayQueueIndex = newIndex == 0 ? 0 : newIndex - 1;
            if (playState != 0)
            {
                if (CurrentSong!.IsPlayAvailable)
                {
                    await SetSource(CurrentSong!.Path);
                    _ = UpdateLyric(CurrentSong!.Lyric);
                    _smtcManager.SetButtonsEnabled(true, true, true, true);
                    Play();
                }
                else
                {
                    HandleSongNotAvailable();
                }
            }
        }
        else if (index < PlayQueueIndex)
        {
            PlayQueueIndex--;
        }

        queue.RemoveAt(index);
        _playQueueLength--;
        for (var i = index; i < queue.Count; i++)
        {
            queue[i].Index = i;
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
    public void MoveUpSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        if (index > 0)
        {
            (queue[index - 1], queue[index]) = (queue[index], queue[index - 1]);
            queue[index].Index = index;
            queue[index - 1].Index = index - 1;
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
    public void MoveDownSong(IndexedPlayQueueSong info)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var index = info.Index;
        if (index < queue.Count - 1)
        {
            (queue[index + 1], queue[index]) = (queue[index], queue[index + 1]);
            queue[index].Index = index;
            queue[index + 1].Index = index + 1;
            if (index == PlayQueueIndex)
            {
                PlayQueueIndex++;
            }
        }
    }

    public void InsertSongsToPlayQueue(List<IBriefSongInfoBase> songs, int insertIndex)
    {
        var queue = ShuffleMode ? ShuffledPlayQueue : PlayQueue;
        var actualIndex = Math.Min(insertIndex, queue.Count);
        foreach (var song in songs)
        {
            queue.Insert(actualIndex, new IndexedPlayQueueSong(actualIndex, song));
            actualIndex++;
        }
        _playQueueLength += songs.Count;
        for (var i = actualIndex; i < queue.Count; i++)
        {
            queue[i].Index = i;
        }
        if (actualIndex <= PlayQueueIndex)
        {
            PlayQueueIndex += songs.Count;
        }
        if (ShuffleMode)
        {
            foreach (var song in songs)
            {
                PlayQueue.Add(new IndexedPlayQueueSong(PlayQueue.Count, song));
            }
        }
    }

    /// <summary>
    /// 从文件路径创建音频流
    /// </summary>
    /// <param name="path">文件路径</param>
    private void CreateStreamFromPath(string path)
    {
        FreeCurrentStreams();
        const BassFlags flags =
            BassFlags.Unicode | BassFlags.Float | BassFlags.AsyncFile | BassFlags.Decode;

        _currentStream = CurrentSong!.IsOnline
            ? Bass.CreateStream(path, 0, flags, null)
            : Bass.CreateStream(path, 0, 0, flags);
        if (_currentStream == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Bass流失败: {error}, 文件: {path}");
        }

        _tempoStream = IsExclusiveMode
            ? BassFx.TempoCreate(_currentStream, BassFlags.Decode)
            : BassFx.TempoCreate(_currentStream, BassFlags.FxFreeSource);
        if (_tempoStream == 0)
        {
            var error = Bass.LastError;
            _logger.ZLogInformation($"创建Tempo流失败: {error}");
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }

        SetPlaybackSpeed(PlaySpeed);

        Bass.ChannelSetSync(_tempoStream, SyncFlags.End, 0, _syncEndCallback);
        Bass.ChannelSetSync(_tempoStream, SyncFlags.Stalled, 0, _syncFailCallback);

        var lengthBytes = Bass.ChannelGetLength(_tempoStream);
        var lengthSeconds = Bass.ChannelBytes2Seconds(_tempoStream, lengthBytes);
        TotalPlayingTime = TimeSpan.FromSeconds(lengthSeconds);
    }

    /// <summary>
    /// 释放当前音频流
    /// </summary>
    private void FreeCurrentStreams()
    {
        if (_tempoStream != 0)
        {
            Bass.StreamFree(_tempoStream);
            _tempoStream = 0;
        }
        if (_currentStream != 0)
        {
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }
    }

    /// <summary>
    /// 更新循环模式
    /// </summary>
    public void RepeatModeUpdate(object _1, RoutedEventArgs _2)
    {
        RepeatMode = (byte)((RepeatMode + 1) % 3);
    }

    /// <summary>
    /// 静音按钮点击事件
    /// </summary>
    public void MuteButton_Click(object _1, RoutedEventArgs _2)
    {
        IsMute = !IsMute;
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="volume"></param>
    private void SetVolumeValue(double volume)
    {
        if (_tempoStream != 0)
        {
            Bass.ChannelSetAttribute(_tempoStream, ChannelAttribute.Volume, (float)volume);
        }
    }

    /// <summary>
    /// 设置播放队列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="list"></param>
    public void SetPlayQueue(string name, IEnumerable<IBriefSongInfoBase> list)
    {
        if (PlayQueue.Count != list.AsValueEnumerable().Count() || PlayQueueName != name)
        {
            PlayQueueName = name;
            PlayQueue =
            [
                .. list.AsValueEnumerable()
                    .Select((song, index) => new IndexedPlayQueueSong(index, song)),
            ];

            _playQueueLength = list.AsValueEnumerable().Count();

            if (ShuffleMode)
            {
                UpdateShufflePlayQueue();
                for (var i = 0; i < ShuffledPlayQueue.Count; i++)
                {
                    ShuffledPlayQueue[i].Index = i;
                }
            }
        }
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    public void SetShuffledPlayQueue(string name, IEnumerable<IBriefSongInfoBase> list)
    {
        if (
            ShuffleMode == false
            || PlayQueue.Count != list.AsValueEnumerable().Count()
            || PlayQueueName != name
        )
        {
            ShuffleMode = true;
            PlayQueueName = name;
            PlayQueue =
            [
                .. list.AsValueEnumerable()
                    .Select((song, index) => new IndexedPlayQueueSong(index, song)),
            ];
            _playQueueLength = list.AsValueEnumerable().Count();

            UpdateShufflePlayQueue();
            for (var i = 0; i < ShuffledPlayQueue.Count; i++)
            {
                ShuffledPlayQueue[i].Index = i;
            }
        }
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
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
    public void UpdateCurrentLyricIndex(double currentTime)
    {
        if (CurrentLyric.Count == 0)
        {
            return;
        }
        var newIndex = GetCurrentLyricIndex(currentTime);
        if (newIndex != _currentLyricIndex)
        {
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = false;
            }
            _currentLyricIndex = newIndex;

            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyric.Count)
            {
                CurrentLyric[_currentLyricIndex].IsCurrent = true;
                CurrentLyricContent = CurrentLyric[_currentLyricIndex].Content;
            }
        }
    }

    /// <summary>
    /// 设置播放位置的内部实现
    /// </summary>
    /// <param name="targetTimeSeconds">目标时间(秒)</param>
    private async void SetPlaybackPositionInternal(double targetTimeSeconds)
    {
        if (_tempoStream != 0)
        {
            var targetBytes = Bass.ChannelSeconds2Bytes(_tempoStream, targetTimeSeconds);
            var result = Bass.ChannelSetPosition(_tempoStream, targetBytes);
            if (!result)
            {
                var error = Bass.LastError;
                if (error == Errors.Position)
                {
                    var retryCount = 0;
                    while (!result && retryCount < 10) // 最多重试10次
                    {
                        await Task.Delay(100);
                        result = Bass.ChannelSetPosition(_tempoStream, targetBytes);
                        retryCount++;
                    }
                }
            }

            CurrentPlayingTime = TimeSpan.FromSeconds(targetTimeSeconds);
            if (TotalPlayingTime.TotalMilliseconds > 0)
            {
                CurrentPosition =
                    100
                    * (CurrentPlayingTime.TotalMilliseconds / TotalPlayingTime.TotalMilliseconds);
            }
            UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
        }
    }

    /// <summary>
    /// 播放上一曲
    /// </summary>
    public void PlayPreviousSong()
    {
        try
        {
            var newIndex = PlayQueueIndex > 0 ? PlayQueueIndex - 1 : PlayQueueIndex;

            if (RepeatMode == 1)
            {
                newIndex = (PlayQueueIndex + _playQueueLength - 1) % _playQueueLength;
            }

            PlaySongByIndex(newIndex);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"播放上一曲失败");
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

            if (RepeatMode == 1)
            {
                newIndex = (PlayQueueIndex + 1) % _playQueueLength;
                isLast = false;
            }

            PlaySongByIndex(newIndex, isLast);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"播放下一曲失败");
        }
    }

    /// <summary>
    /// 播放按钮更新
    /// </summary>
    public void PlayPauseUpdate()
    {
        if (PlayState == 0)
        {
            Play();
        }
        else
        {
            Pause();
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
                ShuffledPlayQueue[i].Index = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    ShuffledPlayQueue
                        .AsValueEnumerable()
                        .FirstOrDefault(info =>
                            CurrentSongHighlightExtensions.IsSameSong(CurrentSong, info.Song)
                        )
                        ?.Index ?? 0;
            }
        }
        else
        {
            ShuffledPlayQueue.Clear();
            for (var i = 0; i < PlayQueue.Count; i++)
            {
                PlayQueue[i].Index = i;
            }
            if (CurrentSong is not null)
            {
                PlayQueueIndex =
                    PlayQueue
                        .AsValueEnumerable()
                        .FirstOrDefault(info =>
                            CurrentSongHighlightExtensions.IsSameSong(CurrentSong, info.Song)
                        )
                        ?.Index ?? 0;
            }
        }
    }

    /// <summary>
    /// 设置播放速度
    /// </summary>
    /// <param name="speed"></param>
    private void SetPlaybackSpeed(double speed)
    {
        if (_tempoStream != 0)
        {
            var tempoPercent = (speed - 1.0) * 100.0;
            Bass.ChannelSetAttribute(_tempoStream, ChannelAttribute.Tempo, (float)tempoPercent);
        }
    }

    /// <summary>
    /// 随机播放队列更新
    /// </summary>
    public void UpdateShufflePlayQueue()
    {
        ShuffledPlayQueue = [.. PlayQueue.AsValueEnumerable().OrderBy(x => Guid.NewGuid())];
    }

    /// <summary>
    /// 清空播放队列
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        CurrentBriefSong = null;
        CurrentSong = null;
        CurrentPlayingTime = TimeSpan.Zero;
        TotalPlayingTime = TimeSpan.Zero;
        PlayQueue.Clear();
        ShuffledPlayQueue.Clear();
        OnPropertyChanged(nameof(PlayQueue));
        CurrentLyric.Clear();
        PlayQueueName = "";
        PlayQueueIndex = 0;
        _playQueueLength = 0;
        Data.RootPlayBarViewModel!.ButtonVisibility = Visibility.Collapsed;
        Data.RootPlayBarViewModel!.Availability = false;
        _smtcManager.SetButtonsEnabled(false, false, false, false);
        _ = FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
    }

    /// <summary>
    /// 按下滑动条事件
    /// </summary>
    public void ProgressLock(object sender, PointerRoutedEventArgs _)
    {
        _lockable = true;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
    }

    /// <summary>
    /// 键盘按下移动滑动条事件
    /// </summary>
    public void ProgressLock(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Left && e.Key != VirtualKey.Right)
        {
            return;
        }
        _lockable = true;
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
        UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
    }

    /// <summary>
    /// 滑动滑动条事件
    /// </summary>
    public void SliderUpdate(object sender, PointerRoutedEventArgs _)
    {
        CurrentPlayingTime = TimeSpan.FromMilliseconds(
            ((Slider)sender).Value * TotalPlayingTime.TotalMilliseconds / 100
        );
        UpdateCurrentLyricIndex(CurrentPlayingTime.TotalMilliseconds);
    }

    /// <summary>
    /// 松开滑动条更新播放进度
    /// </summary>
    public void ProgressUpdate(object sender, PointerRoutedEventArgs _)
    {
        var targetTimeSeconds = ((Slider)sender).Value * TotalPlayingTime.TotalSeconds / 100;
        SetPlaybackPosition(targetTimeSeconds, false);
    }

    /// <summary>
    /// 键盘松开移动滑动条事件
    /// </summary>
    public void ProgressUpdate(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Left && e.Key != VirtualKey.Right)
        {
            return;
        }
        var targetTimeSeconds = ((Slider)sender).Value * TotalPlayingTime.TotalSeconds / 100;
        SetPlaybackPosition(targetTimeSeconds, false);
    }

    public void LyricProgressUpdate(double time)
    {
        var targetTimeSeconds = time / 1000.0;
        SetPlaybackPosition(targetTimeSeconds);
    }

    public void SkipBack10sButton_Click(object _1, RoutedEventArgs _2)
    {
        if (_tempoStream == 0)
        {
            return;
        }
        var currentPositionBytes = Bass.ChannelGetPosition(_tempoStream);
        var currentPositionSeconds = Bass.ChannelBytes2Seconds(_tempoStream, currentPositionBytes);
        var newPositionSeconds = Math.Max(0, currentPositionSeconds - 10);
        SetPlaybackPosition(newPositionSeconds);
    }

    public void SkipForw30sButton_Click(object _1, RoutedEventArgs _2)
    {
        if (_tempoStream == 0)
        {
            return;
        }
        var currentPositionBytes = Bass.ChannelGetPosition(_tempoStream);
        var currentPositionSeconds = Bass.ChannelBytes2Seconds(_tempoStream, currentPositionBytes);
        var newPositionSeconds = Math.Min(
            TotalPlayingTime.TotalSeconds,
            currentPositionSeconds + 30
        );
        SetPlaybackPosition(newPositionSeconds);
    }

    /// <summary>
    /// 设置播放位置的通用方法
    /// </summary>
    /// <param name="targetTimeSeconds">目标时间(秒)</param>
    /// <param name="shouldLock">是否需要设置锁定状态</param>
    private void SetPlaybackPosition(double targetTimeSeconds, bool shouldLock = true)
    {
        if (shouldLock)
        {
            _lockable = true;
        }
        SetPlaybackPositionInternal(targetTimeSeconds);
        _lockable = false;
    }

    public void SpeedListView_SelectionChanged(object sender, SelectionChangedEventArgs _)
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

    public void SpeedListView_Loaded(object sender, RoutedEventArgs _)
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
        CurrentLyric = await LyricHelper.GetLyricSlices(lyric);
        _currentLyricIndex = 0;
        CurrentLyricContent = "";

        if (CurrentLyric.Count > 0)
        {
            CurrentLyric[0].IsCurrent = true;
            CurrentLyricContent = CurrentLyric[0].Content;
        }
    }

    public async Task SaveCurrentBriefSongAsync()
    {
        await Task.Run(async () =>
        {
            if (CurrentBriefSong is not null)
            {
                var sourceMode = IBriefSongInfoBase.GetSourceMode(CurrentBriefSong);
                await _localSettingsService.SaveSettingAsync("CurrentBriefSong", CurrentBriefSong);
                await _localSettingsService.SaveSettingAsync("SourceMode", sourceMode);
            }
        });
    }

    /// <summary>
    /// 保存当前播放状态至设置存储
    /// </summary>
    public async Task SaveCurrentStateAsync()
    {
        await FileManager.SavePlayQueueDataAsync(PlayQueue, ShuffledPlayQueue);
        await _localSettingsService.SaveSettingAsync("PlayQueueIndex", PlayQueueIndex);
        await _localSettingsService.SaveSettingAsync("ShuffleMode", ShuffleMode);
        await _localSettingsService.SaveSettingAsync("RepeatMode", RepeatMode);
        await _localSettingsService.SaveSettingAsync("IsMute", IsMute);
        await _localSettingsService.SaveSettingAsync("CurrentVolume", CurrentVolume);
        await _localSettingsService.SaveSettingAsync("PlaySpeed", PlaySpeed);
        await _localSettingsService.SaveSettingAsync("CurrentBriefSong", CurrentBriefSong);
        await SaveCurrentBriefSongAsync();
    }

    /// <summary>
    /// 从设置存储中读取当前播放状态
    /// </summary>
    public async void LoadCurrentStateAsync()
    {
        try
        {
            ShuffleMode = await _localSettingsService.ReadSettingAsync<bool>("ShuffleMode");
            RepeatMode = await _localSettingsService.ReadSettingAsync<byte>("RepeatMode");
            IsMute = await _localSettingsService.ReadSettingAsync<bool>("IsMute");
            IsExclusiveMode = await _localSettingsService.ReadSettingAsync<bool>("IsExclusiveMode");
            if (Settings.NotFirstUsed)
            {
                CurrentVolume = await _localSettingsService.ReadSettingAsync<double>(
                    "CurrentVolume"
                );
                PlaySpeed = await _localSettingsService.ReadSettingAsync<double>("PlaySpeed");
            }

            if (Data.IsFileActivationLaunch)
            {
                Data.RootPlayBarViewModel!.ButtonVisibility = Visibility.Visible;
                Data.RootPlayBarViewModel!.Availability = true;
                HasLoaded = true;
                return;
            }

            (PlayQueue, ShuffledPlayQueue) = await FileManager.LoadPlayQueueDataAsync();
            _playQueueLength = PlayQueue.Count;
            if (_playQueueLength > 0)
            {
                PlayQueueIndex = await _localSettingsService.ReadSettingAsync<int>(
                    "PlayQueueIndex"
                );
                var sourceMode = await _localSettingsService.ReadSettingAsync<short>("SourceMode");
                CurrentBriefSong = sourceMode switch
                {
                    0 => await _localSettingsService.ReadSettingAsync<BriefLocalSongInfo>(
                        "CurrentBriefSong"
                    ),
                    1 => await _localSettingsService.ReadSettingAsync<BriefUnknownSongInfo>(
                        "CurrentBriefSong"
                    ),
                    2 => await _localSettingsService.ReadSettingAsync<BriefCloudOnlineSongInfo>(
                        "CurrentBriefSong"
                    ),
                    _ => null,
                };
                if (CurrentBriefSong is not null)
                {
                    CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                        CurrentBriefSong
                    );
                    await SetSource(CurrentSong!.Path);
                    _ = UpdateLyric(CurrentSong!.Lyric);
                    _smtcManager.SetButtonsEnabled(true, true, true, true);
                }
            }
            Data.RootPlayBarViewModel!.ButtonVisibility =
                CurrentSong is not null && _playQueueLength > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            Data.RootPlayBarViewModel!.Availability =
                CurrentSong is not null && _playQueueLength > 0;
            HasLoaded = true;
        }
        catch (Exception ex)
        {
            CurrentBriefSong = null;
            CurrentSong = null;
            PlayQueue = [];
            ShuffledPlayQueue = [];
            Data.RootPlayBarViewModel!.ButtonVisibility = Visibility.Collapsed;
            Data.RootPlayBarViewModel!.Availability = false;
            HasLoaded = true;
            _logger.ZLogInformation(ex, $"初始化播放状态失败");
        }
    }

    public void Dispose()
    {
        Stop();
        Messenger.Unregister<FontSizeChangeMessage>(this);
        StopWasapiPlayback();
        if (_tempoStream != 0)
        {
            Bass.StreamFree(_tempoStream);
            _tempoStream = 0;
        }
        if (_currentStream != 0)
        {
            Bass.StreamFree(_currentStream);
            _currentStream = 0;
        }
        Bass.Free();
        BassWasapi.Free();
        _smtcManager?.Dispose();
        _syncEndCallback -= OnPlayBackEnded;
        _syncFailCallback -= OnPlaybackFailed;
        GC.SuppressFinalize(this);
    }
}

[MemoryPackable]
public partial class IndexedPlayQueueSong
{
    public int Index { get; set; }
    public IBriefSongInfoBase Song { get; set; } = null!;

    [MemoryPackConstructor]
    public IndexedPlayQueueSong() { }

    public IndexedPlayQueueSong(int index, IBriefSongInfoBase song)
    {
        Index = index;
        Song = song;
    }
}
