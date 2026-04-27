using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.LyricRenderer;

public sealed partial class LyricManager
    : ObservableRecipient,
        IRecipient<FontSizeChangeMessage>,
        IRecipient<LyricOffsetChangeMessage>,
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
    /// 全局歌词偏移毫秒数，正数表示歌词显示延后，负数表示歌词显示提前
    /// </summary>
    private double _globalLyricOffset = Settings.GlobalLyricOffset;

    /// <summary>
    /// 是否已手动调整（手动调整后将脱离全局偏移控制）
    /// </summary>
    private bool _isManuallyAdjusted = false;

    /// <summary>
    /// 手动调整的歌词偏移毫秒数
    /// </summary>
    private double _manualLyricOffset = 0;

    /// <summary>
    /// 总偏移毫秒数
    /// </summary>
    private double TotalLyricOffset =>
        _isManuallyAdjusted ? _manualLyricOffset : _globalLyricOffset;

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
        Messenger.Register<FontSizeChangeMessage>(this);
        Messenger.Register<LyricOffsetChangeMessage>(this);
        _state = state;
    }

    public void Receive(FontSizeChangeMessage message)
    {
        foreach (var slice in CurrentLyricSlices)
        {
            slice.UpdateStyle();
        }
    }

    public void Receive(LyricOffsetChangeMessage message)
    {
        _globalLyricOffset = message.OffsetMilliseconds;
        if (!_isManuallyAdjusted)
        {
            UpdateLyricAdjustDisplay();
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
            _isManuallyAdjusted = false;
            _manualLyricOffset = 0;
            UpdateLyricAdjustDisplay();

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
        var offset = TotalLyricOffset;
        for (var i = 0; i < CurrentLyricSlices.Count; i++)
        {
            if (CurrentLyricSlices[i].StartTime + offset > currentTime)
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

    public Task AddLyricAdjust()
    {
        if (!_isManuallyAdjusted)
        {
            _manualLyricOffset = _globalLyricOffset;
            _isManuallyAdjusted = true;
        }
        _manualLyricOffset += 300;
        UpdateLyricAdjustDisplay();
        return Task.CompletedTask;
    }

    public Task SubtractLyricAdjust()
    {
        if (!_isManuallyAdjusted)
        {
            _manualLyricOffset = _globalLyricOffset;
            _isManuallyAdjusted = true;
        }
        _manualLyricOffset -= 300;
        UpdateLyricAdjustDisplay();
        return Task.CompletedTask;
    }

    private void UpdateLyricAdjustDisplay()
    {
        var total = TotalLyricOffset;
        LyricAdjustDisplayStr =
            total == 0 ? "0.0s" : $"{(total > 0 ? "+" : "-")}{Math.Abs(total) / 1000:F1}s";
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
            _isManuallyAdjusted = false;
            _manualLyricOffset = 0;
            UpdateLyricAdjustDisplay();
            CurrentLyricSlices.Clear();
        });
    }

    public void Dispose()
    {
        Messenger.Unregister<FontSizeChangeMessage>(this);
        Messenger.Unregister<LyricOffsetChangeMessage>(this);
        GC.SuppressFinalize(this);
    }
}
