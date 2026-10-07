using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class MetadataControl : Control
{
    public Style TextBlockStyle
    {
        get => (Style)GetValue(TextBlockStyleProperty);
        set => SetValue(TextBlockStyleProperty, value);
    }

    public static readonly DependencyProperty TextBlockStyleProperty = DependencyProperty.Register(
        nameof(TextBlockStyle),
        typeof(Style),
        typeof(MetadataControl),
        new PropertyMetadata(default)
    );

    public SolidColorBrush AlternateBrush
    {
        get => (SolidColorBrush)GetValue(AlternateBrushProperty);
        set => SetValue(AlternateBrushProperty, value);
    }

    public static readonly DependencyProperty AlternateBrushProperty = DependencyProperty.Register(
        nameof(AlternateBrush),
        typeof(SolidColorBrush),
        typeof(MetadataControl),
        new PropertyMetadata(default)
    );

    internal MetadataControl()
    {
        DefaultStyleKey = typeof(MetadataControl);
    }
}
