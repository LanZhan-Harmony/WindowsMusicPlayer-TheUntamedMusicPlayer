using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Helpers;

internal sealed class DragAndDropUIOverride : DependencyObject
{
    public bool IsCaptionVisible
    {
        get => (bool)GetValue(IsCaptionVisibleProperty);
        set => SetValue(IsCaptionVisibleProperty, value);
    }

    private static readonly DependencyProperty IsCaptionVisibleProperty =
        DependencyProperty.Register(
            nameof(IsCaptionVisible),
            typeof(bool),
            typeof(DragAndDropUIOverride),
            new PropertyMetadata(false)
        );

    public bool IsContentVisible
    {
        get => (bool)GetValue(IsContentVisibleProperty);
        set => SetValue(IsContentVisibleProperty, value);
    }

    private static readonly DependencyProperty IsContentVisibleProperty =
        DependencyProperty.Register(
            nameof(IsContentVisible),
            typeof(bool),
            typeof(DragAndDropUIOverride),
            new PropertyMetadata(false)
        );

    public bool IsGlyphVisible
    {
        get => (bool)GetValue(IsGlyphVisibleProperty);
        set => SetValue(IsGlyphVisibleProperty, value);
    }

    private static readonly DependencyProperty IsGlyphVisibleProperty = DependencyProperty.Register(
        nameof(IsGlyphVisible),
        typeof(bool),
        typeof(DragAndDropUIOverride),
        new PropertyMetadata(false)
    );
}
