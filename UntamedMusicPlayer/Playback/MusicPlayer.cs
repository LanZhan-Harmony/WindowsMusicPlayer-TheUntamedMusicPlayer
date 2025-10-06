using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Media;
using Windows.Media.Playback;
using ZLinq;

namespace UntamedMusicPlayer.Playback;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();
    private readonly AudioEngine _audioEngine;
    private readonly PlayQueueManager _queueManager;
    private readonly SMTCManager _smtcManager;
    private readonly LyricManager _lyricManager;

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
    public SharedPlaybackState State { get; } = new();

    /// <summary>
    /// 是否已经加载完成
    /// </summary>
    public bool HasLoaded { get; private set; } = false;

    /// <summary>
    /// 通知底部播放栏按钮状态变更事件
    /// </summary>
    public event Action<bool>? BarViewAvailabilityChanged;

    public MusicPlayer()
        : base(StrongReferenceMessenger.Default)
    {
        _audioEngine = new AudioEngine(State);
        _queueManager = new PlayQueueManager(State);
        _smtcManager = new SMTCManager(State);
        _lyricManager = new LyricManager(State);

        // 设置事件处理
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.PlaybackFailed += OnPlaybackFailed;
        _queueManager.OnPlayQueueEmpty += ClearPlayQueue;
        _queueManager.OnCurrentSongRemoved += OnCurrentSongRemoved;
        _smtcManager.ButtonPressed += OnSMTCButtonPressed;

        LoadStateAsync();
    }

    /// <summary>
    /// 播放结束回调
    /// </summary>
    private void OnPlaybackEnded()
    {
        if (_updatable)
        {
            if (State.RepeatMode == RepeatState.RepeatOne)
            {
                PlaySongByIndex(State.PlayQueueIndex, false);
                return;
            }
            PlayNextSong();
        }
    }

    /// <summary>
    /// 播放失败回调
    /// </summary>
    private void OnPlaybackFailed()
    {
        HandleSongNotAvailable();
    }

    /// <summary>
    /// 播放不可用歌曲处理
    /// </summary>
    private void HandleSongNotAvailable()
    {
        _logger.SongPlaybackError(State.CurrentSong!.Title);
        if (RepeatState.RepeatOne == State.RepeatMode || State.CurrentSong.IsOnline)
        {
            Stop();
            return;
        }
        State.CurrentBriefSong?.IsPlayAvailable = false;
        _failedCount++;
        if (_failedCount >= 3)
        {
            _failedCount = 0;
            Stop();
            return;
        }
        PlayNextSong();
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
                PlayPauseUpdate();
                break;
            case SystemMediaTransportControlsButton.Previous:
                PlayPreviousSong();
                break;
            case SystemMediaTransportControlsButton.Next:
                PlayNextSong();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 按歌曲信息播放歌曲
    /// </summary>
    /// <param name="info"></param>
    public void PlaySongByInfo(IBriefSongInfoBase info)
    {
        var index =
            _queueManager
                .CurrentQueue.AsValueEnumerable()
                .FirstOrDefault(song => song.Song == info)
                ?.Index ?? 0;
        PlaySongByIndex(index, false);
    }

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="shouldStop"></param>
    private async void PlaySongByIndex(int index, bool shouldStop = false)
    {
        Stop();
        State.PlayState = MediaPlaybackState.Buffering;
        var songToPlay = _queueManager.CurrentQueue[index];
        State.CurrentBriefSong = songToPlay.Song;
        State.CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
            songToPlay.Song
        );
        State.PlayQueueIndex = index;
        if (!State.CurrentSong.IsPlayAvailable)
        {
            HandleSongNotAvailable();
            return;
        }
        await SetSource();
        _lyricManager.GetSongLyric();
        _smtcManager.SetButtonsEnabled(true, true, true, true);
        if (shouldStop)
        {
            State.PlayState = MediaPlaybackState.Paused;
        }
        else
        {
            Play();
        }
    }

    /// <summary>
    /// 设置播放源
    /// </summary>
    /// <returns></returns>
    private async Task SetSource()
    {
        BarViewAvailabilityChanged?.Invoke(true);
        _audioEngine.LoadSong();
        _smtcManager.UpdateMediaInfo();
        await _smtcManager.SetCoverImageAndUpdateAsync();
    }

    /// <summary>
    /// 播放上一曲
    /// </summary>
    public void PlayPreviousSong()
    {
        var prevIndex = _queueManager.GetPreviousSongIndex();
        PlaySongByIndex(prevIndex, false);
    }

    /// <summary>
    /// 播放下一曲
    /// </summary>
    public void PlayNextSong()
    {
        var (nextIndex, isLast) = _queueManager.GetNextSongIndex();
        PlaySongByIndex(nextIndex, isLast);
    }

    /// <summary>
    /// 清空播放队列(回调)
    /// </summary>
    public void ClearPlayQueue()
    {
        Stop();
        State.CurrentSong = null;
        State.CurrentBriefSong = null;
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
        PlaySongByIndex(State.PlayQueueIndex, shouldStop);
    }

    /// <summary>
    /// 切换播放/暂停状态
    /// </summary>
    public void PlayPauseUpdate()
    {
        if (State.PlayState == MediaPlaybackState.Paused)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        _positionUpdateTimer = new Timer(
            UpdateTimerHandler,
            null,
            TimeSpan.FromMilliseconds(0),
            TimeSpan.FromMilliseconds(250)
        );
        _audioEngine.Play();
        State.PlayState = MediaPlaybackState.Playing;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Playing);
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        _audioEngine.Pause();
        State.PlayState = MediaPlaybackState.Paused;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Paused);
        _positionUpdateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _positionUpdateTimer = null;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        _audioEngine.Stop();
        State.PlayState = MediaPlaybackState.Paused;
        _lyricManager.Reset();
        _positionUpdateTimer?.Dispose();
        _positionUpdateTimer = null;
    }

    /// <summary>
    /// 计时器更新回调
    /// </summary>
    /// <param name="_"></param>
    private async void UpdateTimerHandler(object? _)
    {
        if (!_updatable || State.PlayState != MediaPlaybackState.Playing)
        {
            return;
        }
        await _audioEngine.UpdatePosition();
        _lyricManager.UpdateCurrentLyric();
        _smtcManager.UpdateTimelinePosition();
    }

    /// <summary>
    /// 快退10秒
    /// </summary>
    public void SkipBack10s()
    {
        _updatable = false;
        _audioEngine.SkipBack10s();
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    /// <summary>
    /// 快进30秒
    /// </summary>
    public void SkipForward30s()
    {
        _updatable = false;
        _audioEngine.SkipForward30s();
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    /// <summary>
    /// 鼠标或键盘拖动进度条时调用, 仅更新歌词
    /// </summary>
    /// <param name="time"></param>
    public void PositionUpdate(TimeSpan time)
    {
        _updatable = false;
        State.CurrentPlayingTime = time;
        _lyricManager.UpdateCurrentLyric();
    }

    /// <summary>
    /// 鼠标或键盘拖动进度条完成后调用, 设置播放位置
    /// </summary>
    /// <param name="time"></param>
    public void SetPosition(TimeSpan time)
    {
        State.CurrentPlayingTime = time;
        _audioEngine.SetPosition(time.TotalSeconds);
        _lyricManager.UpdateCurrentLyric();
        _updatable = true;
    }

    public async void LoadStateAsync()
    {
        await State.LoadStateAsync();
        if (Data.IsFileActivationLaunch)
        {
            BarViewAvailabilityChanged?.Invoke(true);
            HasLoaded = true;
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
        HasLoaded = true;
    }

    public async Task SaveStateAsync()
    {
        await State.SaveStateAsync();
        await _queueManager.SaveStateAsync();
    }

    public void Dispose()
    {
        Stop();
        _audioEngine.PlaybackEnded -= OnPlaybackEnded;
        _audioEngine.PlaybackFailed -= OnPlaybackFailed;
        _audioEngine.Dispose();
        _queueManager.OnPlayQueueEmpty -= ClearPlayQueue;
        _queueManager.OnCurrentSongRemoved -= OnCurrentSongRemoved;
        _smtcManager.ButtonPressed -= OnSMTCButtonPressed;
        _smtcManager.Dispose();
        GC.SuppressFinalize(this);
    }
}
