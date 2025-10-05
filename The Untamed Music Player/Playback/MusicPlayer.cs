using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using Windows.Media;
using Windows.Media.Playback;
using Windows.System.Threading;
using ZLinq;
using ZLogger;

namespace The_Untamed_Music_Player.Playback;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MusicPlayer>();
    private readonly PlaybackState _state;
    private readonly PlayQueueManager _queueManager;
    private readonly AudioEngine _audioEngine;
    private readonly SMTCManager _smtcManager;

    /// <summary>
    /// 线程计时器
    /// </summary>
    private ThreadPoolTimer? _positionUpdateTimer;

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    private int _currentLyricIndex = 0;

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
        _state = new PlaybackState();
        _queueManager = new PlayQueueManager(_state);
        _audioEngine = new AudioEngine(_state);
        _smtcManager = new SMTCManager(_state);

        // 设置事件处理
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.PlaybackFailed += OnPlaybackFailed;

        InitializeSmtc();
        LoadCurrentStateAsync();
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
            UpdateLyric(_state.CurrentSong!.Lyric);
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

    public async void UpdateLyric(string lyric)
    {
        CurrentLyric = await LyricHelper.GetLyricSlices(lyric);
        _currentLyricIndex = 0;

        if (CurrentLyric.Count > 0)
        {
            CurrentLyric[0].IsCurrent = true;
            CurrentLyricContent = CurrentLyric[0].Content;
        }
        else
        {
            CurrentLyricContent = "";
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
        _currentLyricIndex = 0;
        CurrentLyricContent = "";
        _positionUpdateTimer?.Cancel();
        _positionUpdateTimer = null;
    }

    public async void LoadCurrentStateAsync() => throw new NotImplementedException();

    public void Dispose()
    {
        _audioEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}
