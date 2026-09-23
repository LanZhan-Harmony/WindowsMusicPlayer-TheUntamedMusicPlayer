using CommunityToolkit.Mvvm.ComponentModel;

namespace UntamedMediaPlayer.Core.Models.Settings;

public sealed class AppSettings : ObservableObject
{
    public LibrarySettings LibrarySettings { get; set; } = new();
    public MusicPlaybackSettings MusicPlaybackSettings { get; set; } = new();
    public VideoPlaybackSettings VideoPlaybackSettings { get; set; } = new();
    public LyricSettings LyricSettings { get; set; } = new();
    public PersonalizationSettings PersonalizationSettings { get; set; } = new();
    public PrivacySettings PrivacySettings { get; set; } = new();
    public DeveloperSettings DeveloperSettings { get; set; } = new();
}
