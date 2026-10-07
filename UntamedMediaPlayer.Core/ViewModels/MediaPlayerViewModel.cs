using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UntamedMediaPlayer.Core.ViewModels;

/// <summary>
/// Shared state surface consumed by the recovered media transport templates.
/// The media engine is not part of the migrated project yet, so these
/// properties provide stable bindable defaults until the engine is connected.
/// </summary>
public class MediaPlayerViewModel : ObservableObject
{
    public bool IsPlaying { get; set; }
    public bool IsPlayingVideo { get; set; }
    public bool IsShuffleEnabled { get; set; }
    public bool IsZoomedToFill { get; set; }
    public TimeSpan Duration { get; set; }
    public object? RepeatMode { get; set; }
    public object? DisplayMode { get; set; }
    public object? LanguageSelection { get; set; }

    public MediaPlayerCommand ToggleShuffleModeCommand { get; } = new("Shuffle");
    public MediaPlayerCommand MoveToPreviousTrackCommand { get; } = new("Previous");
    public MediaPlayerCommand SkipBackwardCommand { get; } = new("Skip backward");
    public MediaPlayerCommand TogglePlayPauseCommand { get; } = new("Play");
    public MediaPlayerCommand SkipForwardCommand { get; } = new("Skip forward");
    public MediaPlayerCommand MoveToNextTrackCommand { get; } = new("Next");
    public MediaPlayerCommand ToggleRepeatModeCommand { get; } = new("Repeat");
    public MediaPlayerCommand ToggleFullScreenModeCommand { get; } = new("Full screen");
    public MediaPlayerCommand ToggleMiniPlayerModeCommand { get; } = new("Mini player");
    public MediaPlayerCommand ShowFilePropertiesCommand { get; } = new("Properties");
    public MediaPlayerCommand EditWithClipChampCommand { get; } = new("Edit");
    public MediaPlayerCommand ShowEqualizerCommand { get; } = new("Equalizer");
    public MediaPlayerCommand ChangePlaybackRateCommand { get; } = new("Playback rate");
    public MediaPlayerCommand RotateMediaCommand { get; } = new("Rotate");
    public MediaPlayerCommand ToggleZoomToFillCommand { get; } = new("Zoom");
}

/// <summary>Small command adapter used by the migrated XAML until real commands are wired.</summary>
public sealed class MediaPlayerCommand : ObservableObject, ICommand
{
    private bool _isEnabled = true;

    public MediaPlayerCommand(string label) => Label = label;

    public string Label { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => IsEnabled;

    public void Execute(object? parameter) { }
}
