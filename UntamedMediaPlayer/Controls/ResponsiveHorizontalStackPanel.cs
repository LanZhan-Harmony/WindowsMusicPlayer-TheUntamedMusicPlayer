using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace UntamedMediaPlayer.Controls;

/// <summary>Compatibility panel for the responsive track row recovered from UWP.</summary>
public sealed partial class ResponsiveHorizontalStackPanel : Panel
{
    public static double GetStretchFactor(DependencyObject obj) =>
        (double)obj.GetValue(StretchFactorProperty);

    public static void SetStretchFactor(DependencyObject obj, double value) =>
        obj.SetValue(StretchFactorProperty, value);

    public static readonly DependencyProperty StretchFactorProperty =
        DependencyProperty.RegisterAttached(
            "StretchFactor",
            typeof(double),
            typeof(ResponsiveHorizontalStackPanel),
            new PropertyMetadata(1d)
        );

    public static double GetUnstretchedWidth(DependencyObject obj) =>
        (double)obj.GetValue(UnstretchedWidthProperty);

    public static void SetUnstretchedWidth(DependencyObject obj, double value) =>
        obj.SetValue(UnstretchedWidthProperty, value);

    public static readonly DependencyProperty UnstretchedWidthProperty =
        DependencyProperty.RegisterAttached(
            "UnstretchedWidth",
            typeof(double),
            typeof(ResponsiveHorizontalStackPanel),
            new PropertyMetadata(0d)
        );

    public static int GetFallOffOrder(DependencyObject obj) =>
        (int)obj.GetValue(FallOffOrderProperty);

    public static void SetFallOffOrder(DependencyObject obj, int value) =>
        obj.SetValue(FallOffOrderProperty, value);

    public static readonly DependencyProperty FallOffOrderProperty =
        DependencyProperty.RegisterAttached(
            "FallOffOrder",
            typeof(int),
            typeof(ResponsiveHorizontalStackPanel),
            new PropertyMetadata(0)
        );

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d;
        var height = 0d;
        foreach (UIElement? child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += Math.Max(child.DesiredSize.Width, GetUnstretchedWidth(child));
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(
            double.IsPositiveInfinity(availableSize.Width)
                ? width
                : Math.Min(width, availableSize.Width),
            double.IsPositiveInfinity(availableSize.Height)
                ? height
                : Math.Min(height, availableSize.Height)
        );
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0d;
        foreach (UIElement? child in Children)
        {
            var width = Math.Max(child.DesiredSize.Width, GetUnstretchedWidth(child));
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }
        return finalSize;
    }
}
