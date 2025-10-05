using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System.Threading;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Playback;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();
    private readonly SharedPlaybackState _state;
    private readonly PlayQueueManager _queueManager;
    private readonly AudioEngine _audioEngine;
    private readonly SMTCManager _smtcManager;
    private readonly LyricManager _lyricManager;

    /// <summary>
    /// 线程计时器
    /// </summary>
    private ThreadPoolTimer? _positionUpdateTimer;

    /// <summary>
    /// 是否已经加载完成
    /// </summary>
    public bool HasLoaded { get; private set; } = false;

    public MusicPlayer()
        : base(StrongReferenceMessenger.Default)
    {
        _state = new SharedPlaybackState();
        _queueManager = new PlayQueueManager(_state);
        _audioEngine = new AudioEngine(_state);
        _smtcManager = new SMTCManager(_state);
        _lyricManager = new LyricManager(_state);

        // 设置事件处理
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.PlaybackFailed += OnPlaybackFailed;

        InitializeSmtc();
        LoadStateAsync();
    }

    private void InitializeSmtc()
    {
        _smtcManager.ButtonPressed += button =>
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
        };
    }

    private void OnPlaybackEnded() => throw new NotImplementedException();

    private void OnPlaybackFailed() => throw new NotImplementedException();

    private void HandleSongNotAvailable() => throw new NotImplementedException();

    /// <summary>
    /// 按索引播放歌曲
    /// </summary>
    /// <param name="index"></param>
    /// <param name="shouldStop"></param>
    private async void PlaySongByIndex(int index, bool shouldStop = false)
    {
        Stop();
        _state.PlayState = MediaPlaybackState.Buffering;
        var songToPlay = _queueManager.CurrentQueue[index];
        _state.CurrentBriefSong = songToPlay.Song;
        _state.CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
            songToPlay.Song
        );
        _state.PlayQueueIndex = index;
        if (_state.CurrentSong!.IsPlayAvailable)
        {
            await SetSource();
            _lyricManager.GetSongLyric();
            _smtcManager.SetButtonsEnabled(true, true, true, true);
            if (shouldStop)
            {
                _state.PlayState = MediaPlaybackState.Paused;
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

    private async Task SetSource()
    {
        try
        {
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Visible;
            Data.RootPlayBarViewModel?.Availability = true;
            _audioEngine.LoadSong();
            _smtcManager.UpdateMediaInfo();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"SetSource失败");
        }
        finally
        {
            await _smtcManager.SetCoverImageAndUpdateAsync();
        }
    }

    public void PlaySongByInfo(IBriefSongInfoBase info)
    {
        var index =
            _queueManager
                .CurrentQueue.AsValueEnumerable()
                .FirstOrDefault(song => song.Song == info)
                ?.Index ?? 0;
        PlaySongByIndex(index, false);
    }

    public void PlayPreviousSong()
    {
        var prevIndex = _queueManager.GetPreviousSongIndex();
        PlaySongByIndex(prevIndex, false);
    }

    public void PlayNextSong()
    {
        var (nextIndex, isLast) = _queueManager.GetNextSongIndex();
        PlaySongByIndex(nextIndex, isLast);
    }

    public void PlayPauseUpdate() { }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        _positionUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer(
            UpdateTimerHandler,
            TimeSpan.FromMilliseconds(250)
        );
        _audioEngine.Play();
        _state.PlayState = MediaPlaybackState.Playing;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Playing);
    }

    private void UpdateTimerHandler(ThreadPoolTimer timer) { }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        _audioEngine.Pause();
        _state.PlayState = MediaPlaybackState.Paused;
        _smtcManager.UpdatePlaybackStatus(MediaPlaybackStatus.Paused);
        _positionUpdateTimer?.Cancel();
        _positionUpdateTimer = null;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        _audioEngine.Stop();
        _state.PlayState = MediaPlaybackState.Paused;
        _lyricManager.Reset();
        _positionUpdateTimer?.Cancel();
        _positionUpdateTimer = null;
    }

    public async void LoadStateAsync()
    {
        try
        {
            await _state.LoadStateAsync();
            if (Data.IsFileActivationLaunch)
            {
                Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Visible;
                Data.RootPlayBarViewModel?.Availability = true;
                return;
            }
            await _queueManager.LoadStateAsync();
            if (_state.CurrentSong is not null)
            {
                await SetSource();
                _lyricManager.GetSongLyric();
                _smtcManager.SetButtonsEnabled(true, true, true, true);
            }
            Data.RootPlayBarViewModel?.ButtonVisibility =
                _state.CurrentSong is not null && _state.PlayQueueCount > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability =
                _state.CurrentSong is not null && _state.PlayQueueCount > 0;
            HasLoaded = true;
        }
        catch
        {
            _state.CurrentSong = null;
            Data.RootPlayBarViewModel?.ButtonVisibility = Visibility.Collapsed;
            Data.RootPlayBarViewModel?.Availability = false;
        }
        finally
        {
            HasLoaded = true;
        }
    }

    public async Task SaveStateAsync()
    {
        await _state.SaveStateAsync();
        await _queueManager.SaveStateAsync();
    }

    public void Dispose()
    {
        _audioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}
