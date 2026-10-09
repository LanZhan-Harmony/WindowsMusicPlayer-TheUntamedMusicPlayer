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

    private static readonly DependencyProperty MediaVisualizationProperty =
        DependencyProperty.Register(
            nameof(MediaVisualization),
            typeof(UIElement),
            typeof(CustomMediaPlayerElement),
            new PropertyMetadata(null)
        );

    public CustomMediaPlayerElement()
    {
        DefaultStyleKey = typeof(CustomMediaPlayerElement);
    }
}
