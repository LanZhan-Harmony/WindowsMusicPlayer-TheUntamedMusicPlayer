using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// Displays album, video, and playlist artwork.
///
/// The original UWP control exposed a few application-specific properties that
/// are still referenced by the recovered resource dictionaries.  They are kept
/// as dependency properties here so styles and templates remain loadable while
/// the media/image service is rebuilt.
/// </summary>
public sealed partial class CoverArtImage : Control
{
    public string? ImageContext
    {
        get => (string?)GetValue(ImageContextProperty);
        set => SetValue(ImageContextProperty, value);
    }

    public static readonly DependencyProperty ImageContextProperty =
        DependencyProperty.Register(
            nameof(ImageContext),
            typeof(string),
            typeof(CoverArtImage),
            new PropertyMetadata(null)
        );

    public string? ImageType
    {
        get => (string?)GetValue(ImageTypeProperty);
        set => SetValue(ImageTypeProperty, value);
    }

    public static readonly DependencyProperty ImageTypeProperty =
        DependencyProperty.Register(
            nameof(ImageType),
            typeof(string),
            typeof(CoverArtImage),
            new PropertyMetadata(null)
        );

    public double NormalizedCenterPoint
    {
        get => (double)GetValue(NormalizedCenterPointProperty);
        set => SetValue(NormalizedCenterPointProperty, value);
    }

    public static readonly DependencyProperty NormalizedCenterPointProperty =
        DependencyProperty.Register(
            nameof(NormalizedCenterPoint),
            typeof(double),
            typeof(CoverArtImage),
            new PropertyMetadata(0.5d)
        );

    public CoverArtImage()
    {
        DefaultStyleKey = typeof(CoverArtImage);
    }
}
