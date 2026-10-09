using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMediaPlayer.Core.ViewModels;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class DurationTextBlock : Control
{
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content),
        typeof(object),
        typeof(DurationTextBlock),
        new PropertyMetadata(null)
    );

    public bool IsRemainingDuration
    {
        get => (bool)GetValue(IsRemainingDurationProperty);
        set => SetValue(IsRemainingDurationProperty, value);
    }

    private static readonly DependencyProperty IsRemainingDurationProperty =
        DependencyProperty.Register(
            nameof(IsRemainingDuration),
            typeof(bool),
            typeof(DurationTextBlock),
            new PropertyMetadata(false)
        );

    public Style? TextStyle
    {
        get => (Style?)GetValue(TextStyleProperty);
        set => SetValue(TextStyleProperty, value);
    }

    private static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(
        nameof(TextStyle),
        typeof(Style),
        typeof(DurationTextBlock),
        new PropertyMetadata(null)
    );

    public MediaPlayerViewModel? ViewModel
    {
        get => (MediaPlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MediaPlayerViewModel),
        typeof(DurationTextBlock),
        new PropertyMetadata(null)
    );

    public DurationTextBlock()
    {
        DefaultStyleKey = typeof(DurationTextBlock);
    }
}
