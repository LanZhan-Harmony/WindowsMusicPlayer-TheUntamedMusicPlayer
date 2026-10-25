using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class CustomMediaPlayerElement : MediaPlayerElement
{
    public UIElement? MediaVisualization
    {
        get => (UIElement?)GetValue(MediaVisualizationProperty);
        set => SetValue(MediaVisualizationProperty, value);
    }

    public static readonly DependencyProperty MediaVisualizationProperty =
        DependencyProperty.Register(
            nameof(MediaVisualization),
            typeof(UIElement),
            typeof(CustomMediaPlayerElement),
            new PropertyMetadata(null)
        );

    public bool AreSubtitlesVisible
    {
        get => (bool)GetValue(AreSubtitlesVisibleProperty);
        set => SetValue(AreSubtitlesVisibleProperty, value);
    }

    public static readonly DependencyProperty AreSubtitlesVisibleProperty =
        DependencyProperty.Register(
            nameof(AreSubtitlesVisible),
            typeof(bool),
            typeof(CustomMediaPlayerElement),
            new PropertyMetadata(false)
        );

    public CustomMediaPlayerElement()
    {
        InitializeComponent();
    }
}
