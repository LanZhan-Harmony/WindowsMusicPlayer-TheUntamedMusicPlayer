using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Playback;

public partial class MusicPlayer : ObservableRecipient, IDisposable
{
    private readonly PlaybackState _state;
    private readonly PlayQueueManager _queueManager;
    private readonly AudioEngine _audioEngine;
    private readonly SystemMediaTransportControlsManager _smtcManager;

    public MusicPlayer()
        : base(StrongReferenceMessenger.Default)
    {
        _state = new PlaybackState();
        _queueManager = new PlayQueueManager(_state);
        _audioEngine = new AudioEngine(_state);
        _smtcManager = new SystemMediaTransportControlsManager();

        // 设置事件处理
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
        _audioEngine.PlaybackFailed += OnPlaybackFailed;

        InitializeSmtc();
        LoadCurrentStateAsync();
    }

    private void OnPlaybackFailed() => throw new NotImplementedException();

    private void OnPlaybackEnded() => throw new NotImplementedException();

    // 公开必要的属性
    public PlaybackState State => _state;
    public PlayQueueManager QueueManager => _queueManager;

    public async void PlaySongByInfo(IBriefSongInfoBase info)
    {
        _audioEngine.Stop();
        _state.PlayState = 2; // 加载中
        _state.CurrentBriefSong = info;
        _state.CurrentSong = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);

        var currentSong = _queueManager.GetCurrentSong();
        _state.PlayQueueIndex = currentSong?.Index ?? 0;

        if (_state.CurrentSong!.IsPlayAvailable)
        {
            if (await _audioEngine.LoadSong(_state.CurrentSong.Path))
            {
                _audioEngine.Play();
                _state.PlayState = 1; // 播放中
            }
        }
        else
        {
            HandleSongNotAvailable();
        }
    }

    public void PlayNextSong()
    {
        var nextIndex = _queueManager.GetNextSongIndex();
        var nextSong = (
            _state.ShuffleMode ? _queueManager.ShuffledPlayQueue : _queueManager.PlayQueue
        )[nextIndex];
        PlaySongByInfo(nextSong.Song);
    }

    public void Dispose() { }
}
