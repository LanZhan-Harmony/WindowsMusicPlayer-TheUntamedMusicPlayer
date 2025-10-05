using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.LyricRenderer;

public partial class LyricManager
    : ObservableRecipient,
        IRecipient<FontSizeChangeMessage>,
        IDisposable
{
    private readonly SharedPlaybackState _state;

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
    public partial List<LyricSlice> CurrentLyricSlices { get; set; } = [];

    public LyricManager(SharedPlaybackState state)
        : base(StrongReferenceMessenger.Default)
    {
        _state = state;
        _state.PropertyChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedPlaybackState.CurrentPlayingTime):
                UpdateCurrentLyric();
                break;
        }
    }

    public void Receive(FontSizeChangeMessage message)
    {
        foreach (var slice in CurrentLyricSlices)
        {
            slice.UpdateStyle();
        }
    }

    /// <summary>
    /// 获取当前歌曲歌词
    /// </summary>
    public async void GetSongLyric()
    {
        CurrentLyricSlices = await LyricParser.GetLyricSlices(_state.CurrentSong!.Lyric);
        _currentLyricIndex = 0;

        if (CurrentLyricSlices.Count > 0)
        {
            CurrentLyricSlices[0].IsCurrent = true;
            CurrentLyricContent = CurrentLyricSlices[0].Content;
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
        for (var i = 0; i < CurrentLyricSlices.Count; i++)
        {
            if (CurrentLyricSlices[i].Time > currentTime)
            {
                return i > 0 ? i - 1 : 0;
            }
        }
        return CurrentLyricSlices.Count - 1;
    }

    /// <summary>
    /// 更新当前歌词状态
    /// </summary>
    /// <param name="currentTime">当前播放时间（毫秒）</param>
    public void UpdateCurrentLyric()
    {
        var currentTime = _state.CurrentPlayingTime.TotalMilliseconds;
        if (CurrentLyricSlices.Count == 0)
        {
            return;
        }
        var newIndex = GetCurrentLyricIndex(currentTime);
        if (newIndex != _currentLyricIndex)
        {
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyricSlices.Count)
            {
                CurrentLyricSlices[_currentLyricIndex].IsCurrent = false;
            }
            _currentLyricIndex = newIndex;

            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyricSlices.Count)
            {
                CurrentLyricSlices[_currentLyricIndex].IsCurrent = true;
                CurrentLyricContent = CurrentLyricSlices[_currentLyricIndex].Content;
            }
        }
    }

    /// <summary>
    /// 重置歌词状态
    /// </summary>
    public void Reset()
    {
        _currentLyricIndex = 0;
        CurrentLyricContent = "";
        CurrentLyricSlices.Clear();
    }

    public void Dispose()
    {
        Messenger.Unregister<FontSizeChangeMessage>(this);
        _state.PropertyChanged -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}
