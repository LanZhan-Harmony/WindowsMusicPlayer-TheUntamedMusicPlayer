using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Applies the configured minimum and fixed dimensions to its associated element.
/// </summary>
internal sealed class TextScaleFactorBehavior : Behavior<FrameworkElement>
{
    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    private static readonly DependencyProperty MinHeightProperty = DependencyProperty.Register(
        nameof(MinHeight),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(0d, OnLayoutValueChanged)
    );

    public double Width
    {
        get => (double)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    private static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(double.NaN, OnLayoutValueChanged)
    );

    public double Height
    {
        get => (double)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    private static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
        nameof(Height),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(double.NaN, OnLayoutValueChanged)
    );

    protected override void OnAttached()
    {
        base.OnAttached();
        ApplyLayoutValues();
    }

    private static void OnLayoutValueChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is TextScaleFactorBehavior behavior)
        {
            behavior.ApplyLayoutValues();
        }
    }

    private void ApplyLayoutValues()
    {
        if (AssociatedObject is not { } element)
        {
            return;
        }

        if (MinHeight > 0)
        {
            element.MinHeight = MinHeight;
        }

        if (!double.IsNaN(Width))
        {
            element.Width = Width;
        }

        if (!double.IsNaN(Height))
        {
            element.Height = Height;
        }
    }
}
