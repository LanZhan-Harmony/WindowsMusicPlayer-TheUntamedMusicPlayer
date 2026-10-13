using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UntamedMediaPlayer.Core.Enums;

namespace UntamedMediaPlayer.Core.ViewModels;

/// <summary>
/// Shared state surface consumed by the recovered media transport templates.
/// The media engine is not part of the migrated project yet, so these
/// properties provide stable bindable defaults until the engine is connected.
/// </summary>
public partial class MediaPlayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsPlayingVideo { get; set; }

    [ObservableProperty]
    public partial bool IsShuffleEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsZoomedToFill { get; set; }

    [ObservableProperty]
    public partial TimeSpan Duration { get; set; }

    [ObservableProperty]
    public partial RepeatMode RepeatMode { get; set; }

    [ObservableProperty]
    public partial DisplayMode DisplayMode { get; set; }

    [ObservableProperty]
    public partial string? LanguageSelection { get; set; }

    [RelayCommand]
    public void TogglePlayPause() { }

    [RelayCommand]
    public void ToggleShuffleMode() { }

    [RelayCommand]
    public void ToggleRepeatMode() { }

    [RelayCommand]
    public void ToggleFullScreenMode() { }

    [RelayCommand]
    public void ToggleMiniPlayerMode() { }

    [RelayCommand]
    public void ToggleZoomToFill() { }

    [RelayCommand]
    public void MoveToPreviousTrack() { }

    [RelayCommand]
    public void MoveToNextTrack() { }

    [RelayCommand]
    public void SkipBackward() { }

    [RelayCommand]
    public void SkipForward() { }

    [RelayCommand]
    public void ShowFileProperties() { }

    [RelayCommand]
    public void EditWithClipChamp() { }

    [RelayCommand]
    public void ShowEqualizer() { }

    [RelayCommand]
    public void ChangePlaybackRate() { }

    [RelayCommand]
    public void RotateMedia() { }
}
