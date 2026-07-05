using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Core.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Services;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Media;
using Windows.Media.Playback;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Playback;

public sealed partial class MusicPlayer : IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();
    private readonly AudioEngine _audioEngine;
    private readonly PlayQueueManager _queueManager;
    private readonly SMTCManager _smtcManager;
    private readonly LyricManager _lyricManager;
    private readonly IAppStateService _appStateService;

    /// <summary>
    /// 扩展执行会话，用于防止后台暂停
    /// </summary>
    private ExtendedExecutionSession? _extendedExecutionSession;

    /// <summary>
    /// 线程计时器
    /// </summary>
    private Timer? _positionUpdateTimer;

    /// <summary>
    /// 是否启用状态更新
    /// </summary>
    private bool _updatable = true;

    /// <summary>
    /// 播放失败计数
    /// </summary>
    private byte _failedCount = 0;

    /// <summary>
    /// 播放器共享状态
    /// </summary>
    public SharedPlaybackState State { get; }

    public PlayQueueManager QueueManager => _queueManager;

    public LyricManager LyricManager => _lyricManager;

    private readonly TaskCompletionSource _loadTcs = new();

    /// <summary>
    /// 等待加载完成
    /// </summary>
    /// <returns></returns>
    public Task WhenLoadedAsync() => _loadTcs.Task;

    /// <summary>
    /// 通知底部播放栏按钮状态变更事件
    /// </summary>
    public event Action<bool>? BarViewAvailabilityChanged;

    public MusicPlayer(IAppStateService appStateService)
    {
        _appStateService = appStateService;
        State = new();
        _audioEngine = new(State);
        _queueManager = new(State);
        _smtcManager = new(State);
        _lyricManager = new(State);

        // 设置事件处理
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.PlaybackFailed += OnPlaybackFailed;
        _queueManager.OnPlayQueueEmpty += ClearPlayQueue;
        _queueManager.OnCurrentSongRemoved += OnCurrentSongRemoved;
        _smtcManager.ButtonPressed += OnSMTCButtonPressed;
        _smtcManager.PlaybackPositionChangeRequested += OnSMTCPlaybackPositionChangeRequested;

        RunFireAndForget(LoadStateAsync());
    }

    /// <summary>
    /// 播放结束回调
    /// </summary>
    private void OnPlaybackEnded() => RunFireAndForget(OnPlaybackEndedAsync());

    private async Task OnPlaybackEndedAsync()
    {
        if (_updatable)
        {
            if (State.RepeatMode == RepeatState.RepeatOne)
            {
                State.CurrentPlayingTime = TimeSpan.Zero;
                await _audioEngine.SetPositionAsync(0);
                await Play();
                _lyricManager.UpdateCurrentLyric();
                return;
            }
            await PlayNextSongAsync();
        }
    }

    /// <summary>
    /// 播放失败回调
    /// </summary>
    private void OnPlaybackFailed()
    {
        RunFireAndForget(HandleSongNotAvailableAsync());
    }

    /// <summary>
    /// 播放不可用歌曲处理
    /// </summary>
    private async Task HandleSongNotAvailableAsync()
    {
        _logger.SongPlaybackError(State.CurrentSong!.Title);
        if (RepeatState.RepeatOne == State.RepeatMode || State.CurrentSong.IsOnline)
        {
            await Stop();
            return;
        }
        State.CurrentBriefSong?.IsPlayAvailable = false;
        _failedCount++;
        if (_failedCount >= 3)
        {
            _failedCount = 0;
            await Stop();
            return;
        }
        await PlayNextSongAsync();
    }

    /// <summary>
    /// SMTC按钮按下回调
    /// </summary>
    /// <param name="button"></param>
    private void OnSMTCButtonPressed(SystemMediaTransportControlsButton button)
    {
        switch (button)
        {
            case SystemMediaTransportControlsButton.Play:
            case SystemMediaTransportControlsButton.Pause:
                RunFireAndForget(PlayPauseUpdateAsync());
                break;
            case SystemMediaTransportControlsButton.Previous:
                RunFireAndForget(PlayPreviousSongAsync());
                break;
            case SystemMediaTransportControlsButton.Next:
                RunFireAndForget(PlayNextSongAsync());
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// SMTC播放位置更改请求回调
    /// </summary>
    /// <param name="time"></param>
    private void OnSMTCPlaybackPositionChangeRequested(TimeSpan time) =>
        RunFireAndForget(SetPositionAsync(time));

    private async Task SetPositionAsync(TimeSpan time)
    {
        if (time > State.TotalPlayingTime)
        {
            time = State.TotalPlayingTime;
        }
        State.CurrentPlayingTime = time;
        await _audioEngine.SetPositionAsync(time.TotalSeconds);
        _lyricManager.UpdateCurrentLyric();
    }

    /// <summary>
    /// 按歌曲信息播放歌曲
    /// </summary>
    /// <param name="info"></param>
    public void PlaySongByInfo(IBriefSongInfoBase info) =>
        RunFireAndForget(PlaySongByInfoAsync(info));

    public Task PlaySongByInfoAsync(IBriefSongInfoBase info)
    {
        var index =
            _queueManager
                .CurrentQueue.AsValueEnumerable()
                .FirstOrDefault(song => song.Song == info)
                ?.Index
            ?? 0;
        return PlaySongByIndexAsync(index, false);
    }

    /// <summary>
    /// 按索引歌曲信息播放歌曲
    /// </summary>
    /// <param name="info"></param>
    public void PlaySongByIndexedInfo(IndexedPlayQueueSong info) =>
        RunFireAndForget(PlaySongByIndexedInfoAsync(info));

    public Task PlaySongByIndexedInfoAsync(IndexedPlayQueueSong info)
    {
        return PlaySongByIndexAsync(info.Index);
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="shouldStop"></param>
    private async Task PlaySongByIndexAsync(int index, bool shouldStop = false)
    {
        await Stop();
        State.PlayState = MediaPlaybackState.Buffering;
        var songToPlay = _queueManager.CurrentQueue[index];
        State.CurrentBriefSong = songToPlay.Song;
        State.CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
            songToPlay.Song
        );
        State.PlayQueueIndex = index;
        if (!State.CurrentSong.IsPlayAvailable)
        {
            await HandleSongNotAvailableAsync();
            State.PlayState = MediaPlaybackState.Paused;
            return;
        }
        var couldPlay = await SetSource();
        _lyricManager.GetSongLyric();
        _smtcManager.SetButtonsEnabled(true, true, true, true);
        if (!shouldStop && couldPlay)
        {
            await Play();
        }
        else
        {
            State.PlayState = MediaPlaybackState.Paused;
        }
    }

    /// <summary>
    /// 设置播放源
    /// </summary>
    /// <returns></returns>
    private async Task<bool> SetSource()
    {
        BarViewAvailabilityChanged?.Invoke(true);
        if (!await _audioEngine.LoadSongAsync())
        {
            return false;
        }
        _smtcManager.UpdateMediaInfo();
        await _smtcManager.SetCoverImageAndUpdateAsync();
        return true;
    }

    /// <summary>
    /// 播放上一曲
    /// </summary>
    public void PlayPreviousSong() => RunFireAndForget(PlayPreviousSongAsync());

    public Task PlayPreviousSongAsync()
    {
        var prevIndex = _queueManager.GetPreviousSongIndex();
        return PlaySongByIndexAsync(prevIndex, false);
    }

    /// <summary>
    /// 播放下一曲
    /// </summary>
    public void PlayNextSong() => RunFireAndForget(PlayNextSongAsync());

    public Task PlayNextSongAsync()
    {
        var (nextIndex, isLast) = _queueManager.GetNextSongIndex();
        return PlaySongByIndexAsync(nextIndex, isLast);
    }

    /// <summary>
    /// 清空播放队列(回调)
    /// </summary>
    public void ClearPlayQueue() => RunFireAndForget(ClearPlayQueueAsync());

    public async Task ClearPlayQueueAsync()
    {
        await Stop();
        State.CurrentPlayingTime = TimeSpan.Zero;
        State.TotalPlayingTime = TimeSpan.Zero;
        _queueManager.Reset();
        _lyricManager.Reset();
        _smtcManager.SetButtonsEnabled(false, false, false, false);
        BarViewAvailabilityChanged?.Invoke(false);
    }

    /// <summary>
    /// 移除当前歌曲回调
    /// </summary>
    private void OnCurrentSongRemoved()
    {
        var shouldStop = !(State.PlayState == MediaPlaybackState.Playing);
        RunFireAndForget(PlaySongByIndexAsync(State.PlayQueueIndex, shouldStop));
    }

    /// <summary>
    /// 切换播放/暂停状态
    /// </summary>
    public void PlayPauseUpdate() => RunFireAndForget(PlayPauseUpdateAsync());

    public async Task PlayPauseUpdateAsync()
    {
        if (State.PlayState == MediaPlaybackState.Paused)
        {
            await Play();
        }
        else
        {
            await Pause();
        }
    }

    /// <summary>
    /// 切换静音状态
    /// </summary>
    public void MuteUnmuteUpdate() => State.IsMute = !State.IsMute;

    /// <summary>
    /// 播放
    /// </summary>
    public async Task Play()
    {
        if (await _audioEngine.PlayAsync())
        {
            RunFireAndForget(RequestExtendedExecutionAsync());
            _positionUpdateTimer = new Timer(
                UpdateTimerHandler,
                null,
                TimeSpan.FromMilliseconds(0),
                TimeSpan.FromMilliseconds(250)
            );
            State.PlayState = MediaPlaybackState.Playing;
            _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Playing);
        }
        else
        {
            State.PlayState = MediaPlaybackState.Paused;
        }
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public async Task Pause()
    {
        await _audioEngine.PauseAsync();
        ClearExtendedExecution();
        State.PlayState = MediaPlaybackState.Paused;

        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Paused);
        _positionUpdateTimer?.Dispose();
        _positionUpdateTimer = null;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public async Task Stop()
    {
        await _audioEngine.StopAsync();
        ClearExtendedExecution();
        State.PlayState = MediaPlaybackState.Paused;

        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Paused);
        _lyricManager.Reset();
        _positionUpdateTimer?.Dispose();
        _positionUpdateTimer = null;
    }

    /// <summary>
    /// 计时器更新回调
    /// </summary>
    private void UpdateTimerHandler(object? _) => RunFireAndForget(UpdateTimerAsync());

    private async Task UpdateTimerAsync()
    {
        if (!_updatable || State.PlayState != MediaPlaybackState.Playing)
        {
            return;
        }
        await _audioEngine.UpdatePositionAsync();
        _lyricManager.UpdateCurrentLyric();
        _smtcManager.UpdateTimelinePosition();
    }

    /// <summary>
    /// 设置独占模式
    /// </summary>
    /// <param name="isExclusive"></param>
    public void SetExclusiveMode(bool isExclusive) =>
        RunFireAndForget(SetExclusiveModeAsync(isExclusive));

    public async Task SetExclusiveModeAsync(bool isExclusive)
    {
        if (State.IsExclusiveMode == isExclusive)
        {
            return;
        }
        _updatable = false;
        await _audioEngine.SetExclusiveModeAsync(
            isExclusive,
            State.PlayState == MediaPlaybackState.Playing
        );
        _updatable = true;
    }

    /// <summary>
    /// 快退10秒
    /// </summary>
    public void SkipBack10s() => RunFireAndForget(SkipBack10sAsync());

    public async Task SkipBack10sAsync()
    {
        _updatable = false;
        await _audioEngine.SkipBack10sAsync();
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    /// <summary>
    /// 快进30秒
    /// </summary>
    public void SkipForward30s() => RunFireAndForget(SkipForward30sAsync());

    public async Task SkipForward30sAsync()
    {
        _updatable = false;
        await _audioEngine.SkipForward30sAsync();
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    /// <summary>
    /// 鼠标或键盘拖动进度条时调用, 仅更新歌词
    /// </summary>
    /// <param name="time"></param>
    public void LyricUpdateByPercentage(double sliderValue, bool stopUpdate)
    {
        if (stopUpdate)
        {
            _updatable = false;
        }
        State.CurrentPlayingTime = TimeSpan.FromMilliseconds(
            sliderValue * State.TotalPlayingTime.TotalMilliseconds / 100
        );
        _lyricManager.UpdateCurrentLyric();
    }

    /// <summary>
    /// 鼠标或键盘拖动进度条完成后调用, 设置播放位置
    /// </summary>
    /// <param name="time"></param>
    public void SetPositionByPercentage(double sliderValue) =>
        RunFireAndForget(SetPositionByPercentageAsync(sliderValue));

    public async Task SetPositionByPercentageAsync(double sliderValue)
    {
        State.CurrentPlayingTime = TimeSpan.FromMilliseconds(
            sliderValue * State.TotalPlayingTime.TotalMilliseconds / 100
        );
        await _audioEngine.SetPositionAsync(State.CurrentPlayingTime.TotalSeconds);
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    /// <summary>
    /// 点击歌词时调用, 设置播放位置
    /// </summary>
    /// <param name="time"></param>
    public void LyricPositionUpdate(double time) =>
        RunFireAndForget(LyricPositionUpdateAsync(time));

    public async Task LyricPositionUpdateAsync(double time)
    {
        _updatable = false;
        State.CurrentPlayingTime = TimeSpan.FromMilliseconds(time);
        await _audioEngine.SetPositionAsync(time / 1000);
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    public async Task LoadStateAsync()
    {
        await State.LoadStateAsync();
        if (_appStateService.IsFileActivationLaunch)
        {
            BarViewAvailabilityChanged?.Invoke(true);
            _loadTcs.TrySetResult();
            return;
        }
        await _queueManager.LoadStateAsync();
        if (State.CurrentSong is not null)
        {
            await SetSource();
            _lyricManager.GetSongLyric();
            _smtcManager.SetButtonsEnabled(true, true, true, true);
        }
        BarViewAvailabilityChanged?.Invoke(
            State.CurrentSong is not null && State.PlayQueueCount > 0
        );
        _loadTcs.TrySetResult();
    }

    public async Task SaveStateAsync()
    {
        await State.SaveStateAsync();
        await _queueManager.SaveStateAsync();
    }

    public void Dispose()
    {
        Stop().GetAwaiter().GetResult();
        _audioEngine.PlaybackEnded -= OnPlaybackEnded;
        _audioEngine.PlaybackFailed -= OnPlaybackFailed;
        _audioEngine.Dispose();
        _queueManager.OnPlayQueueEmpty -= ClearPlayQueue;
        _queueManager.OnCurrentSongRemoved -= OnCurrentSongRemoved;
        _smtcManager.ButtonPressed -= OnSMTCButtonPressed;
        _smtcManager.PlaybackPositionChangeRequested -= OnSMTCPlaybackPositionChangeRequested;
        _smtcManager.Dispose();
        ClearExtendedExecution();
        GC.SuppressFinalize(this);
    }

    private async Task RequestExtendedExecutionAsync()
    {
        if (_extendedExecutionSession is not null)
        {
            return;
        }

        var newSession = new ExtendedExecutionSession
        {
            Reason = ExtendedExecutionReason.Unspecified,
            Description = "Playing Music",
        };

        newSession.Revoked += ExtendedExecutionSession_Revoked;

        var result = await newSession.RequestExtensionAsync();
        if (result == ExtendedExecutionResult.Allowed)
        {
            _extendedExecutionSession = newSession;
        }
        else
        {
            newSession.Dispose();
        }

        // 同时阻止系统进入睡眠（非显示器关闭后的睡眠）
        _ = ExternFunction.SetThreadExecutionState(
            (uint)(
                ExternFunction.EXECUTION_STATE.ES_SYSTEM_REQUIRED
                | ExternFunction.EXECUTION_STATE.ES_CONTINUOUS
            )
        );
    }

    private void ExtendedExecutionSession_Revoked(
        object sender,
        ExtendedExecutionRevokedEventArgs args
    )
    {
        ClearExtendedExecution();
    }

    private void ClearExtendedExecution()
    {
        if (_extendedExecutionSession is not null)
        {
            _extendedExecutionSession.Revoked -= ExtendedExecutionSession_Revoked;
            _extendedExecutionSession.Dispose();
            _extendedExecutionSession = null;
        }

        // 允许系统正常进入睡眠
        _ = ExternFunction.SetThreadExecutionState(
            (uint)ExternFunction.EXECUTION_STATE.ES_CONTINUOUS
        );
    }

    private void RunFireAndForget(Task task)
    {
        _ = RunFireAndForgetAsync(task);
    }

    private async Task RunFireAndForgetAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"播放器异步操作失败");
        }
    }
}
