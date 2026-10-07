using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

public sealed partial class MediaTypeIcon : Control
{
    public object? MediaType
    {
        get => GetValue(MediaTypeProperty);
        set => SetValue(MediaTypeProperty, value);
    }

    public static readonly DependencyProperty MediaTypeProperty = DependencyProperty.Register(
        nameof(MediaType),
        typeof(object),
        typeof(MediaTypeIcon),
        new PropertyMetadata(null)
    );

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
        nameof(IsPlaying),
        typeof(bool),
        typeof(MediaTypeIcon),
        new PropertyMetadata(false)
    );

    public MediaTypeIcon()
    {
        DefaultStyleKey = typeof(MediaTypeIcon);
    }
}
