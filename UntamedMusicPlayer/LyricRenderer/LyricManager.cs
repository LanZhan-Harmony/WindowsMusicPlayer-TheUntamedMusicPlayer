using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.LyricRenderer;

public sealed partial class LyricManager
    : ObservableRecipient,
        IRecipient<FontSizeChangeMessage>,
        IDisposable
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();
    private readonly SharedPlaybackState _state;

    /// <summary>
    /// 当前歌词切片在集合中的索引
    /// </summary>
    [ObservableProperty]
    public partial int CurrentLyricIndex { get; set; } = 0;

    /// <summary>
    /// 当前歌词内容
    /// </summary>
    [ObservableProperty]
    public partial string CurrentLyricContent { get; set; } = "";

    /// <summary>
    /// 歌词偏移毫秒数，正数表示歌词显示延后，负数表示歌词显示提前
    /// </summary>
    public double LyricAdjustMilliseconds { get; set; } = 0;

    /// <summary>
    /// 歌词偏移显示字符串
    /// </summary>
    [ObservableProperty]
    public partial string LyricAdjustDisplayStr { get; set; } = "0.0s";

    /// <summary>
    /// 当前歌词切片集合
    /// </summary>
    [ObservableProperty]
    public partial List<LyricSlice> CurrentLyricSlices { get; set; } = [];

    public LyricManager(SharedPlaybackState state)
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        _state = state;
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
        var slices = await LyricParser.GetLyricSlices(
            _state.CurrentSong!.Lyric,
            _state.CurrentSong!.Duration
        );

        _dispatcher.TryEnqueue(() =>
        {
            CurrentLyricSlices = slices;
            CurrentLyricIndex = 0;

            if (CurrentLyricSlices.Count > 0)
            {
                CurrentLyricSlices[0].IsCurrent = true;
                CurrentLyricContent = CurrentLyricSlices[0].Content;
            }
            else
            {
                CurrentLyricContent =
                    $"{_state.CurrentSong!.Title}\n{"Lyric_NoLyric".GetLocalized()}";
            }
        });
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
            if (CurrentLyricSlices[i].StartTime > currentTime)
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
        if (CurrentLyricSlices.Count == 0)
        {
            return;
        }

        var newIndex = GetCurrentLyricIndex(_state.CurrentPlayingTime.TotalMilliseconds);
        if (newIndex == CurrentLyricIndex)
        {
            return;
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (newIndex < 0 || newIndex >= CurrentLyricSlices.Count)
            {
                return;
            }

            if (CurrentLyricIndex >= 0 && CurrentLyricIndex < CurrentLyricSlices.Count)
            {
                CurrentLyricSlices[CurrentLyricIndex].IsCurrent = false;
            }

            var newSlice = CurrentLyricSlices[newIndex];
            newSlice.IsCurrent = true;
            CurrentLyricContent = newSlice.Content;
            CurrentLyricIndex = newIndex;
        });
    }

    public async Task AddLyricAdjust()
    {
        LyricAdjustMilliseconds += 300;
        LyricAdjustDisplayStr =
            LyricAdjustMilliseconds == 0
                ? "0.0s"
                : $"{(LyricAdjustMilliseconds > 0 ? "+" : "-")}{Math.Abs(LyricAdjustMilliseconds) / 1000:F1}s";
        await Task.Run(() =>
        {
            foreach (var slice in CurrentLyricSlices)
            {
                slice.StartTime += 300;
                slice.EndTime += 300;
            }
        });
    }

    public async Task SubtractLyricAdjust()
    {
        LyricAdjustMilliseconds -= 300;
        LyricAdjustDisplayStr =
            LyricAdjustMilliseconds == 0
                ? "0.0s"
                : $"{(LyricAdjustMilliseconds > 0 ? "+" : "-")}{Math.Abs(LyricAdjustMilliseconds) / 1000:F1}s";
        await Task.Run(() =>
        {
            foreach (var slice in CurrentLyricSlices)
            {
                slice.StartTime = Math.Max(0, slice.StartTime - 300);
                slice.EndTime = Math.Max(0, slice.EndTime - 300);
            }
        });
    }

    /// <summary>
    /// 重置歌词状态
    /// </summary>
    public void Reset()
    {
        _dispatcher.TryEnqueue(() =>
        {
            CurrentLyricIndex = 0;
            CurrentLyricContent = "";
            LyricAdjustMilliseconds = 0;
            LyricAdjustDisplayStr = "0.0s";
            CurrentLyricSlices.Clear();
        });
    }

    public void Dispose()
    {
        Messenger.Unregister<FontSizeChangeMessage>(this);
        GC.SuppressFinalize(this);
    }
}
