using CommunityToolkit.Mvvm.ComponentModel;
using The_Untamed_Music_Player.Contracts.Models;
using Windows.Media.Playback;

namespace The_Untamed_Music_Player.Playback;

public partial class PlaybackState : ObservableObject
{
    [ObservableProperty]
    public partial ShuffleState ShuffleMode { get; set; } = ShuffleState.Normal;

    [ObservableProperty]
    public partial RepeatState RepeatMode { get; set; } = RepeatState.NoRepeat;

    [ObservableProperty]
    public partial MediaPlaybackState PlayState { get; set; } = MediaPlaybackState.Paused;

    [ObservableProperty]
    public partial double CurrentVolume { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsMute { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExclusiveMode { get; set; } = false;

    [ObservableProperty]
    public partial double PlaySpeed { get; set; } = 1.0;

    [ObservableProperty]
    public partial TimeSpan CurrentPlayingTime { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial TimeSpan TotalPlayingTime { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial int PlayQueueIndex { get; set; } = -1;

    [ObservableProperty]
    public partial int PlayQueueCount { get; set; } = 0;

    [ObservableProperty]
    public partial IBriefSongInfoBase? CurrentBriefSong { get; set; }

    [ObservableProperty]
    public partial IDetailedSongInfoBase? CurrentSong { get; set; }
}

public enum ShuffleState
{
    /// <summary>
    /// 正常模式
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 随机模式
    /// </summary>
    Shuffled = 1,
}

public enum RepeatState
{
    /// <summary>
    /// 不循环
    /// </summary>
    NoRepeat = 0,

    /// <summary>
    /// 列表循环
    /// </summary>
    RepeatAll = 1,

    /// <summary>
    /// 单曲循环
    /// </summary>
    RepeatOne = 2,
}
