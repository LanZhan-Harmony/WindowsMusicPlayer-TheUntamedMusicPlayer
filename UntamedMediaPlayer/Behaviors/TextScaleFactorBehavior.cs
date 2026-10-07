using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// 根据可用空间缩放文本/内容。简化实现：暴露 MinHeight 用于布局约束。
/// </summary>
public sealed class TextScaleFactorBehavior : Behavior<FrameworkElement>
{
    public double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public static readonly DependencyProperty MinHeightProperty = DependencyProperty.Register(
        nameof(MinHeight),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(0d)
    );

    public double Width
    {
        get => (double)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(double.NaN)
    );

    public double Height
    {
        get => (double)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
        nameof(Height),
        typeof(double),
        typeof(TextScaleFactorBehavior),
        new PropertyMetadata(double.NaN)
    );
}
