using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ZLinq;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class SliderHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(SliderHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(Slider slider) =>
        (bool)slider.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(Slider slider, bool value) =>
        slider.SetValue(AcrylicWorkaroundProperty, value);

    private static void OnAcrylicWorkaroundChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var slider = (Slider)d;
        var weakSlider = new WeakReference<Slider>(slider);

        void loadedHandler(object sender, RoutedEventArgs args)
        {
            if (!weakSlider.TryGetTarget(out var s))
            {
                (sender as Slider)?.Loaded -= loadedHandler;
                return;
            }

            var horizontalThumb = s.FindDescendants()
                .AsValueEnumerable()
                .OfType<Thumb>()
                .FirstOrDefault(t => t.Name == "HorizontalThumb");
            if (horizontalThumb is not null)
            {
                ApplyThumbAcrylic(horizontalThumb);
            }

            var verticalThumb = s.FindDescendants()
                .AsValueEnumerable()
                .OfType<Thumb>()
                .FirstOrDefault(t => t.Name == "VerticalThumb");
            if (verticalThumb is not null)
            {
                ApplyThumbAcrylic(verticalThumb);
            }

            s.Loaded -= loadedHandler;
        }

        slider.Loaded += loadedHandler;
    }

    private static void ApplyThumbAcrylic(Thumb thumb)
    {
        if (ToolTipService.GetToolTip(thumb) is ToolTip toolTip)
        {
            ToolTipHelper.SetAcrylicWorkaround(toolTip, true);
        }
    }
}
