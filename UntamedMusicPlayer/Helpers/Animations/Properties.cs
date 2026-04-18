using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace UntamedMusicPlayer.Helpers.Animations;

public class Properties : DependencyObject
{
    public static string GetStyleKey(DependencyObject obj) =>
        (string)obj.GetValue(StyleKeyProperty);

    public static void SetStyleKey(DependencyObject obj, string value) =>
        obj.SetValue(StyleKeyProperty, value);

    public static readonly DependencyProperty StyleKeyProperty =
        DependencyProperty.RegisterAttached(
            "StyleKey",
            typeof(string),
            typeof(Properties),
            new PropertyMetadata(default(string))
        );

    public static Color GetColor(DependencyObject obj) => (Color)obj.GetValue(ColorProperty);

    public static void SetColor(DependencyObject obj, Color value) =>
        obj.SetValue(ColorProperty, value);

    public static readonly DependencyProperty ColorProperty = DependencyProperty.RegisterAttached(
        "Color",
        typeof(Color),
        typeof(Properties),
        new PropertyMetadata(default(Color))
    );

    public static ZoomHelper GetZoomHelper(DependencyObject obj) =>
        (ZoomHelper)obj.GetValue(ZoomHelperProperty);

    public static void SetZoomHelper(DependencyObject obj, ZoomHelper? value) =>
        obj.SetValue(ZoomHelperProperty, value);

    public static readonly DependencyProperty ZoomHelperProperty =
        DependencyProperty.RegisterAttached(
            "ZoomHelper",
            typeof(ZoomHelper),
            typeof(Properties),
            new PropertyMetadata(default(ZoomHelper))
        );

    public static bool GetUseZoomHelper(DependencyObject obj) =>
        (bool)obj.GetValue(UseZoomHelperProperty);

    public static void SetUseZoomHelper(DependencyObject obj, bool value) =>
        obj.SetValue(UseZoomHelperProperty, value);

    public static readonly DependencyProperty UseZoomHelperProperty =
        DependencyProperty.RegisterAttached(
            "UseZoomHelper",
            typeof(bool),
            typeof(Properties),
            new PropertyMetadata(default(bool), (d, e) => OnUseZoomHelperChanged(d, e))
        );

    private static void OnUseZoomHelperChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is FrameworkElement element && e.NewValue is bool b)
        {
            if (b)
            {
                ZoomHelper helper = new() { TriggerWhenFocused = true };
                SetZoomHelper(element, helper);
                helper.Attach(element);

                if (element is RadioButtons)
                {
                    helper.ZoomInRequested += ZoomOut;
                    helper.ZoomOutRequested += ZoomIn;
                }

                if (element is Slider)
                {
                    helper.Mode = ZoomTriggerMode.Delta;
                    helper.ZoomRequested += Zoom;
                }
            }
            else
            {
                if (GetZoomHelper(element) is ZoomHelper old)
                {
                    old.ZoomInRequested -= ZoomIn;
                    old.ZoomOutRequested -= ZoomOut;
                    old.ZoomRequested -= Zoom;
                    old.Detach(element);
                }

                SetZoomHelper(element, null);
            }

            static void ZoomIn(object? sender, EventArgs e)
            {
                if (sender is not ZoomHelper z)
                {
                    return;
                }

                if (z.Target is RadioButtons r && r.SelectedIndex < GetItemsCount(r) - 1)
                {
                    r.SelectedIndex++;
                }
            }
            static void ZoomOut(object? sender, EventArgs e)
            {
                if (sender is not ZoomHelper z)
                {
                    return;
                }

                if (z.Target is RadioButtons r && r.SelectedIndex > 0)
                {
                    r.SelectedIndex--;
                }
            }

            static void Zoom(object? sender, double e)
            {
                if (sender is not ZoomHelper z)
                {
                    return;
                }

                if (z.Target is Slider s)
                {
                    s.Value += e > 0 ? s.LargeChange : -s.LargeChange;
                }
            }
        }
    }

    private static int GetItemsCount(RadioButtons b)
    {
        if (b.ItemsSource is IList l)
        {
            return l.Count;
        }
        return b.Items.Count;
    }
}
