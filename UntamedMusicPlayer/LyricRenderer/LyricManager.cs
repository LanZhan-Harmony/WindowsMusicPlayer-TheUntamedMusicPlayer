using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.LyricRenderer;

public partial class LyricManager(SharedPlaybackState state)
    : ObservableRecipient(StrongReferenceMessenger.Default),
        IRecipient<FontSizeChangeMessage>,
        IDisposable
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly SharedPlaybackState _state = state;

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
    public void UpdateCurrentLyric()
    {
        if (
            CurrentLyricSlices.Count == 0
            || Data.LyricPage is null && Data.DesktopLyricWindow is null
        )
        {
            return;
        }

        var newIndex = GetCurrentLyricIndex(_state.CurrentPlayingTime.TotalMilliseconds);
        if (newIndex != _currentLyricIndex)
        {
            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyricSlices.Count)
            {
                _dispatcher.TryEnqueue(() =>
                    CurrentLyricSlices[_currentLyricIndex].IsCurrent = false
                );
            }
            _currentLyricIndex = newIndex;

            if (_currentLyricIndex >= 0 && _currentLyricIndex < CurrentLyricSlices.Count)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    CurrentLyricSlices[_currentLyricIndex].IsCurrent = true;
                    CurrentLyricContent = CurrentLyricSlices[_currentLyricIndex].Content;
                });
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
        GC.SuppressFinalize(this);
    }
}
