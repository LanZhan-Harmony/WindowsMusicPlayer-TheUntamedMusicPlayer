using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>Applies the minimum height used by a responsive row to its RowDefinition.</summary>
public sealed partial class RowDefinitionTextScaleFactorBehavior : Behavior<RowDefinition>
{
    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public static readonly DependencyProperty MinHeightProperty = DependencyProperty.Register(
        nameof(MinHeight),
        typeof(double),
        typeof(RowDefinitionTextScaleFactorBehavior),
        new PropertyMetadata(0d, OnMinHeightChanged)
    );

    protected override void OnAttached()
    {
        base.OnAttached();
        ApplyMinHeight();
    }

    private static void OnMinHeightChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is RowDefinitionTextScaleFactorBehavior behavior)
        {
            behavior.ApplyMinHeight();
        }
    }

    private void ApplyMinHeight()
    {
        if (AssociatedObject is { } row && MinHeight > 0)
        {
            row.MinHeight = MinHeight;
        }
    }
}
