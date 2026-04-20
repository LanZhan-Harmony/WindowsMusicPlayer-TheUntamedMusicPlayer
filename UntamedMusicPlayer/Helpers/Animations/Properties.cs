using System.Collections;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace UntamedMusicPlayer.Helpers.Animations;

public sealed class Properties : DependencyObject
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

    public static double GetClickAnimationOffset(DependencyObject obj) =>
        (double)obj.GetValue(ClickAnimationOffsetProperty);

    public static void SetClickAnimationOffset(DependencyObject obj, double value) =>
        obj.SetValue(ClickAnimationOffsetProperty, value);

    public static readonly DependencyProperty ClickAnimationOffsetProperty =
        DependencyProperty.RegisterAttached(
            "ClickAnimationOffset",
            typeof(double),
            typeof(Properties),
            new PropertyMetadata(-2d)
        );

    public static readonly DependencyProperty ColorProperty = DependencyProperty.RegisterAttached(
        "Color",
        typeof(Color),
        typeof(Properties),
        new PropertyMetadata(default(Color))
    );

    public static string GetPointerOverAnimation(DependencyObject obj) =>
        (string)obj.GetValue(PointerOverAnimationProperty);

    public static void SetPointerOverAnimation(DependencyObject obj, string value) =>
        obj.SetValue(PointerOverAnimationProperty, value);

    public static readonly DependencyProperty PointerOverAnimationProperty =
        DependencyProperty.RegisterAttached(
            "PointerOverAnimation",
            typeof(string),
            typeof(Properties),
            new PropertyMetadata(default(string), (d, e) => OnPointerOverAnimationChanged(d, e))
        );

    public static double GetPointerAnimationOffset(DependencyObject obj) =>
        (double)obj.GetValue(PointerAnimationOffsetProperty);

    public static void SetPointerAnimationOffset(DependencyObject obj, double value) =>
        obj.SetValue(PointerAnimationOffsetProperty, value);

    public static readonly DependencyProperty PointerAnimationOffsetProperty =
        DependencyProperty.RegisterAttached(
            "PointerAnimationOffset",
            typeof(double),
            typeof(Properties),
            new PropertyMetadata(-2d)
        );

    public static string GetPointerPressedAnimation(DependencyObject obj) =>
        (string)obj.GetValue(PointerPressedAnimationProperty);

    public static void SetPointerPressedAnimation(DependencyObject obj, string value) =>
        obj.SetValue(PointerPressedAnimationProperty, value);

    public static readonly DependencyProperty PointerPressedAnimationProperty =
        DependencyProperty.RegisterAttached(
            "PointerPressedAnimation",
            typeof(string),
            typeof(Properties),
            new PropertyMetadata(default(string), (d, e) => OnPointerPressedAnimationChanged(d, e))
        );

    private static void OnPointerPressedAnimationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is FrameworkElement f)
        {
            // 1. Remove old handlers
            f.RemoveHandler(UIElement.PointerPressedEvent, (PointerEventHandler)PointerPressed);
            f.RemoveHandler(UIElement.PointerExitedEvent, (PointerEventHandler)PointerExited);

            // 2. Add new handlers
            if (e.NewValue is string s && !string.IsNullOrWhiteSpace(s))
            {
                f.AddHandler(
                    FrameworkElement.PointerPressedEvent,
                    new PointerEventHandler(PointerPressed),
                    true
                );
                f.AddHandler(
                    FrameworkElement.PointerExitedEvent,
                    new PointerEventHandler(PointerExited),
                    true
                );
            }

            static void PointerPressed(object sender, PointerRoutedEventArgs _)
            {
                if (
                    sender is FrameworkElement e
                    && GetPointerPressedAnimation(e) is string s
                    && !string.IsNullOrWhiteSpace(s)
                )
                {
                    DoAnimate(e, s, GetClickAnimationOffset(e));
                }
            }

            static void PointerExited(object sender, PointerRoutedEventArgs _)
            {
                if (
                    sender is FrameworkElement e
                    && GetPointerPressedAnimation(e) is string s
                    && !string.IsNullOrWhiteSpace(s)
                )
                {
                    RestoreAnimate(e, s);
                }
            }

            static void RestoreAnimate(FrameworkElement source, string key)
            {
                var parts = key.Split("|");
                if (
                    source
                        .FindDescendants()
                        .OfType<FrameworkElement>()
                        .FirstOrDefault(fe => fe.Name == parts[0])
                    is FrameworkElement target
                )
                {
                    var duration = 0.35;

                    if (parts.Length > 1 && parts[1] == "Scale")
                    {
                        var v = target.GetElementVisual()!;
                        v.StartAnimation(FluentAnimationHelper.CreatePointerUp(v));
                    }
                    else
                    {
                        var sb = new Storyboard();
                        var ease = new BackEase
                        {
                            Amplitude = 0.5,
                            EasingMode = EasingMode.EaseOut,
                        };
                        // Create translate animation
                        var t = sb.CreateTimeline<DoubleAnimationUsingKeyFrames>(
                                target,
                                TargetProperty.CompositeTransform.TranslateY
                            )
                            .AddKeyFrame(duration, 0, ease);
                        sb.Begin();
                    }
                }
            }

            static void DoAnimate(FrameworkElement source, string key, double offset)
            {
                var parts = key.Split("|");
                if (
                    source
                        .FindDescendants()
                        .OfType<FrameworkElement>()
                        .FirstOrDefault(fe => fe.Name == parts[0])
                    is FrameworkElement target
                )
                {
                    if (parts.Length > 1 && parts[1] == "Scale")
                    {
                        var v = target.GetElementVisual()!;
                        v.StartAnimation(FluentAnimationHelper.CreatePointerDown(v, (float)offset));
                    }
                    else
                    {
                        // Create translate animation
                        var sb = new Storyboard();
                        var ease = new BackEase
                        {
                            Amplitude = 0.5,
                            EasingMode = EasingMode.EaseOut,
                        };
                        sb.CreateTimeline<DoubleAnimationUsingKeyFrames>(
                                target,
                                TargetProperty.CompositeTransform.TranslateY
                            )
                            .AddKeyFrame(0.15, offset)
                            .AddKeyFrame(0.5, 0, ease);

                        sb.Begin();
                    }
                }
            }
        }
    }

    private static void OnPointerOverAnimationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is FrameworkElement f)
        {
            // 1. Remove old handlers
            f.RemoveHandler(UIElement.PointerEnteredEvent, (PointerEventHandler)PointerOverEntered);
            f.RemoveHandler(UIElement.PointerExitedEvent, (PointerEventHandler)PointerOverExited);
            f.RemoveHandler(
                UIElement.PointerCaptureLostEvent,
                (PointerEventHandler)PointerOverExited
            );

            // 2. Add new handlers
            if (e.NewValue is string s && !string.IsNullOrWhiteSpace(s))
            {
                f.AddHandler(
                    FrameworkElement.PointerEnteredEvent,
                    new PointerEventHandler(PointerOverEntered),
                    true
                );
                f.AddHandler(
                    FrameworkElement.PointerExitedEvent,
                    new PointerEventHandler(PointerOverExited),
                    true
                );
                f.AddHandler(
                    FrameworkElement.PointerCaptureLostEvent,
                    new PointerEventHandler(PointerOverExited),
                    true
                );
            }

            static void PointerOverEntered(object sender, PointerRoutedEventArgs _)
            {
                if (
                    sender is FrameworkElement e
                    && GetPointerOverAnimation(e) is string s
                    && !string.IsNullOrWhiteSpace(s)
                )
                {
                    CompButtonAnimate(e, s, GetPointerAnimationOffset(e), true);
                }
            }

            static void PointerOverExited(object sender, PointerRoutedEventArgs _)
            {
                if (
                    sender is FrameworkElement e
                    && GetPointerOverAnimation(e) is string s
                    && !string.IsNullOrWhiteSpace(s)
                )
                {
                    CompButtonAnimate(e, s, 0, true);
                }
            }
        }
    }

    private static void CompButtonAnimate(
        FrameworkElement source,
        string key,
        double offset,
        bool over = false
    )
    {
        var parts = key.Split("|");
        var targets = parts[0].Split(",");

        foreach (var src in targets)
        {
            if (
                source
                    .FindDescendants()
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(fe => fe.Name == src)
                is FrameworkElement target
            )
            {
                if (parts.Length > 1 && parts[1] == "Scale")
                {
                    var v = target.GetElementVisual()!;
                    v.StartAnimation(FluentAnimationHelper.CreatePointerUp(v));
                }
                else
                {
                    var sb = new Storyboard();
                    var ease = new ElasticEase
                    {
                        Oscillations = 2,
                        Springiness = 5,
                        EasingMode = EasingMode.EaseOut,
                    };

                    var hasPressed =
                        string.IsNullOrEmpty(GetPointerPressedAnimation(source)) is false;
                    var duration = hasPressed ? 0.35 : 0.5;

                    // Create translate animation
                    var path =
                        parts.Length > 1 && parts[1] == "X"
                            ? TargetProperty.CompositeTransform.TranslateX
                            : TargetProperty.CompositeTransform.TranslateY;

                    var t = sb.CreateTimeline<DoubleAnimationUsingKeyFrames>(target, path);
                    if (over || hasPressed is false)
                    {
                        if (offset == 0)
                        {
                            t.AddKeyFrame(0.8, offset, ease);
                        }
                        else
                        {
                            t.AddKeyFrame(0.15, offset);
                        }
                    }
                    sb.Begin();
                }
            }
        }
    }

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
