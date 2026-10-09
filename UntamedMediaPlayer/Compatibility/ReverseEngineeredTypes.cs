using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Compatibility;

/// <summary>
/// Small value objects retained for resources recovered from the UWP markup.
/// They intentionally keep the original string value so existing commands and
/// templates can consume the resource without depending on the removed UWP
/// application model types.
/// </summary>
public sealed class PlaybackRate : DependencyObject
{
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(PlaybackRate),
        new PropertyMetadata(null)
    );
}

public sealed class MediaPlayerRepeatMode : PlaybackModeValue { }

public sealed class MediaPlayerDisplayMode : PlaybackModeValue { }

public sealed class ContentDisplayMode : PlaybackModeValue { }

public abstract class PlaybackModeValue : DependencyObject
{
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(PlaybackModeValue),
        new PropertyMetadata(null)
    );

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
