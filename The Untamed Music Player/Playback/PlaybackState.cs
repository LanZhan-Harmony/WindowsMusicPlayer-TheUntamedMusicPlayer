using CommunityToolkit.Mvvm.ComponentModel;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.Playback;

public partial class PlaybackState : ObservableObject
{
    [ObservableProperty]
    public partial bool ShuffleMode { get; set; } = false;

    [ObservableProperty]
    public partial byte RepeatMode { get; set; } = 0;

    [ObservableProperty]
    public partial int PlayQueueIndex { get; set; } = 0;

    [ObservableProperty]
    public partial byte PlayState { get; set; } = 0;

    [ObservableProperty]
    public partial IBriefSongInfoBase? CurrentBriefSong { get; set; }

    [ObservableProperty]
    public partial IDetailedSongInfoBase? CurrentSong { get; set; }

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
    public partial double CurrentPosition { get; set; } = 0;
}
