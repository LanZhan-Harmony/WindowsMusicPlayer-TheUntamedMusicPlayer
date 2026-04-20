using System.Globalization;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace UntamedMusicPlayer.Helpers.Animations;

public sealed class CompositionFactory : DependencyObject
{
    public const double DefaultOffsetDuration = 0.325;

    private static string CENTRE_EXPRESSION =>
        $"({nameof(Vector3)}(this.Target.{nameof(Visual.Size)}.{nameof(Vector2.X)} * {{0}}f, "
        + $"this.Target.{nameof(Visual.Size)}.{nameof(Vector2.Y)} * {{1}}f, 0f))";

    public const string TRANSLATION = "Translation";
    public const string STARTING_VALUE = "this.StartingValue";
    public const string FINAL_VALUE = "this.FinalValue";
    public const int DEFAULT_STAGGER_MS = 83;

    #region Attached Properties
    public static double GetBounceDuration(DependencyObject obj) =>
        (double)obj.GetValue(BounceDurationProperty);

    public static void SetBounceDuration(DependencyObject obj, double value) =>
        obj.SetValue(BounceDurationProperty, value);

    public static readonly DependencyProperty BounceDurationProperty =
        DependencyProperty.RegisterAttached(
            "BounceDuration",
            typeof(double),
            typeof(CompositionFactory),
            new PropertyMetadata(0.15, (d, e) => OnBounceDurationChanged(d, e))
        );

    public static void OnBounceDurationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is FrameworkElement f)
        {
            var v = f.GetElementVisual()!;
            if (e.NewValue is double w && w > 0)
            {
                EnableStandardTranslation(v, w);
            }
            else
            {
                v.Properties.SetImplicitAnimation(TRANSLATION, null);
            }
        }
    }

    public static bool GetEnableBounceScale(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableBounceScaleProperty);
    }

    public static void SetEnableBounceScale(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableBounceScaleProperty, value);
    }

    public static readonly DependencyProperty EnableBounceScaleProperty =
        DependencyProperty.RegisterAttached(
            "EnableBounceScale",
            typeof(bool),
            typeof(CompositionFactory),
            new PropertyMetadata(
                false,
                (d, e) =>
                {
                    if (d is FrameworkElement f)
                    {
                        var v = f.GetElementVisual()!;
                        if (e.NewValue is bool b && b)
                        {
                            EnableStandardTranslation(v, 0.15);
                        }
                        else
                        {
                            v.Properties.SetImplicitAnimation(TRANSLATION, null);
                        }
                    }
                }
            )
        );

    public static Duration GetOpacityDuration(DependencyObject obj)
    {
        return (Duration)obj.GetValue(OpacityDurationProperty);
    }

    public static void SetOpacityDuration(DependencyObject obj, Duration value)
    {
        obj.SetValue(OpacityDurationProperty, value);
    }

    public static readonly DependencyProperty OpacityDurationProperty =
        DependencyProperty.RegisterAttached(
            "OpacityDuration",
            typeof(Duration),
            typeof(CompositionFactory),
            new PropertyMetadata(
                new Duration(TimeSpan.FromSeconds(0)),
                (d, e) =>
                {
                    if (d is FrameworkElement element && e.NewValue is Duration t)
                    {
                        SetOpacityTransition(element, t.HasTimeSpan ? t.TimeSpan : TimeSpan.Zero);
                    }
                }
            )
        );

    public static double GetCornerRadius(DependencyObject obj)
    {
        return (double)obj.GetValue(CornerRadiusProperty);
    }

    public static void SetCornerRadius(DependencyObject obj, double value)
    {
        obj.SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(double),
            typeof(CompositionFactory),
            new PropertyMetadata(
                0d,
                (d, e) =>
                {
                    if (d is FrameworkElement element && e.NewValue is double v)
                    {
                        SetCornerRadius(element, (float)v);
                    }
                }
            )
        );

    #endregion

    public static ImplicitAnimationCollection GetRepositionCollection(Compositor c)
    {
        return c.GetCached(
            "RepoColl",
            () =>
            {
                var g = c.CreateAnimationGroup();
                g.Add(
                    c.CreateVector3KeyFrameAnimation()
                        .SetTarget(nameof(Visual.Offset))
                        .AddKeyFrame(1f, FINAL_VALUE)
                        .SetDuration(DefaultOffsetDuration)
                );

                var s = c.CreateImplicitAnimationCollection();
                s.Add(nameof(Visual.Offset), g);
                return s;
            }
        );
    }

    public static ICompositionAnimationBase CreateScaleAnimation(Compositor c)
    {
        return c.GetCached(
            "ScaleAni",
            () =>
            {
                return c.CreateVector3KeyFrameAnimation()
                    .AddKeyFrame(1f, FINAL_VALUE)
                    .SetDuration(DefaultOffsetDuration)
                    .SetTarget(nameof(Visual.Scale));
            }
        );
    }

    private static void SetOpacityTransition(FrameworkElement e, TimeSpan t)
    {
        if (t.TotalMilliseconds > 0)
        {
            var v = e.GetElementVisual()!;
            var ani = v.CreateScalarKeyFrameAnimation(nameof(Visual.Opacity))
                .AddKeyFrame(1, FINAL_VALUE, v.Compositor.GetLinearEase())
                .SetDuration(t);

            e.SetImplicitAnimation(nameof(Visual.Opacity), ani);
        }
        else
        {
            e.SetImplicitAnimation(nameof(Visual.Opacity), null);
        }
    }

    public static void SetupOverlayPanelAnimation(UIElement e)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;

        var g = v.GetCached(
            "OPA",
            () =>
            {
                var t = v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(1, 0, 200)
                    .SetDuration(0.375);

                var o = CreateFade(v.Compositor, 0, null, 200);
                return v.Compositor.CreateAnimationGroup(t, o);
            }
        );

        e.SetHideAnimation(g);
        e.SetShowAnimation(CreateEntranceAnimation(e, new Vector3(0, 200, 0), 0, 550));
    }

    public static void SetupOverlayPanelAnimationX(UIElement e)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;

        var g = v.GetCached(
            "OPAX",
            () =>
            {
                var t = v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(1, 200, 0)
                    .SetDuration(0.375);

                var o = CreateFade(v.Compositor, 0, null, 200);
                return v.Compositor.CreateAnimationGroup(t, o);
            }
        );

        e.SetHideAnimation(g);
        e.SetShowAnimation(CreateEntranceAnimation(e, new Vector3(200, 0, 0), 0, 550));
    }

    public static void PlayEntrance(
        UIElement target,
        int delayMs = 0,
        int fromOffsetY = 40,
        int fromOffsetX = 0,
        int durationMs = 1000
    )
    {
        var animation = CreateEntranceAnimation(
            target,
            new Vector3(fromOffsetX, fromOffsetY, 0),
            delayMs,
            durationMs
        );
        target.GetElementVisual()!.StartAnimationGroup(animation);
    }

    public static void SetStandardEntrance(FrameworkElement sender, object args)
    {
        if (sender is FrameworkElement e)
        {
            e.SetShowAnimation(CreateEntranceAnimation(e, new Vector3(100, 0, 0), 200));
        }
    }

    public static void PlayStandardEntrance(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement e)
        {
            e.GetElementVisual()!
                .StartAnimationGroup(CreateEntranceAnimation(e, new Vector3(100, 0, 0), 200));
        }
    }

    public static ICompositionAnimationBase CreateEntranceAnimation(
        UIElement target,
        Vector3 from,
        int delayMs,
        int durationMs = 700
    )
    {
        var key = $"CEA{from.X}{from.Y}{delayMs}{durationMs}";
        var c = target.EnableTranslation(true).GetElementVisual()!.Compositor;

        return c.GetCached(
            key,
            () =>
            {
                var delay = TimeSpan.FromMilliseconds(delayMs);
                var e = c.GetCachedFluentEntranceEase();
                var t = c.CreateVector3KeyFrameAnimation()
                    .SetTarget(TRANSLATION)
                    .SetInitialValueBeforeDelay()
                    .SetDelayTime(delay)
                    .AddKeyFrame(0, from)
                    .AddKeyFrame(1, 0, e)
                    .SetDuration(TimeSpan.FromMilliseconds(durationMs));

                var o = CreateFade(c, 1, 0, (int)(durationMs * 0.33), delayMs);
                return c.CreateAnimationGroup(t, o);
            }
        );
    }

    public static void SetCornerRadius(UIElement target, float size)
    {
        var vis = target.GetElementVisual()!;
        var rec = vis.Compositor.CreateRoundedRectangleGeometry();
        rec.CornerRadius = new(size);
        rec.LinkShapeSize(vis);
        var clip = vis.Compositor.CreateGeometricClip(rec);
        vis.Clip = clip;
    }

    public static void PlayEntrance(
        List<UIElement> targets,
        int delayMs = 0,
        int fromOffsetY = 40,
        int fromOffsetX = 0,
        int durationMs = 1000,
        int staggerMs = 83
    )
    {
        var start = delayMs;

        foreach (var target in targets)
        {
            if (target is null)
            {
                continue;
            }

            var animation = CreateEntranceAnimation(
                target,
                new Vector3(fromOffsetX, fromOffsetY, 0),
                start,
                durationMs
            );
            target.GetElementVisual()!.StartAnimationGroup(animation);
            start += staggerMs;
        }
    }

    public static CompositionAnimation CreateFade(
        Compositor c,
        float to,
        float? from,
        int durationMs,
        int delayMs = 0
    )
    {
        var key = $"SFade{to}{from}{durationMs}{delayMs}";
        return c.GetCached(
            key,
            () =>
            {
                var o = c.CreateScalarKeyFrameAnimation().SetTarget(nameof(Visual.Opacity));

                if (from is not null && from.HasValue)
                {
                    o.AddKeyFrame(0, from.Value);
                }

                o.AddKeyFrame(1, to, c.GetCachedFluentEntranceEase())
                    .SetInitialValueBeforeDelay()
                    .SetDelayTime(TimeSpan.FromMilliseconds(delayMs))
                    .SetDuration(TimeSpan.FromMilliseconds(durationMs));

                return o;
            }
        );
    }

    public static ExpressionAnimation StartCentering(Visual v, float x = 0.5f, float y = 0.5f)
    {
        v.StopAnimation(nameof(Visual.CenterPoint));

        var e = v.GetCached(
            $"CP{x}{y}",
            () =>
                v.CreateExpressionAnimation(nameof(Visual.CenterPoint))
                    .SetExpression(
                        string.Format(
                            CENTRE_EXPRESSION,
                            x.ToString(CultureInfo.InvariantCulture.NumberFormat),
                            y.ToString(CultureInfo.InvariantCulture.NumberFormat)
                        )
                    )
        );

        v.StartAnimationGroup(e);
        return e;
    }

    public static void PlayScaleEntrance(
        FrameworkElement target,
        float from,
        float to,
        double duration = 0.6
    )
    {
        var v = target.GetElementVisual()!;

        if (target.Tag is null)
        {
            StartCentering(v);
            target.Tag = target;
        }

        var e = CubicBezierPoints.FluentDecelerate; // v.Compositor.CreateEntranceEasingFunction();

        var t = v.CreateVector3KeyFrameAnimation(nameof(Visual.Scale))
            .AddKeyFrame(0, new Vector3(from, from, 0))
            .AddKeyFrame(1, new Vector3(to, to, 0), e)
            .SetDuration(duration);

        var o = CreateFade(v.Compositor, 1, 0, 200);

        var g = v.Compositor.CreateAnimationGroup(t, o);
        v.StartAnimationGroup(g);
    }

    public static void SetStandardReposition(UIElement e)
    {
        var v = e.GetElementVisual()!;

        var value = v.GetCached(
            "DefaultOffsetAnimation",
            () =>
                v.CreateVector3KeyFrameAnimation(nameof(Visual.Offset))
                    .AddKeyFrame(0, STARTING_VALUE)
                    .AddKeyFrame(1, FINAL_VALUE)
                    .SetDuration(DefaultOffsetDuration)
        );

        v.SetImplicitAnimation(nameof(Visual.Offset), value);
    }

    public static void DisableStandardReposition(FrameworkElement f)
    {
        f.GetElementVisual()!.ImplicitAnimations?.Remove(nameof(Visual.Offset));
    }

    public static Visual EnableStandardTranslation(Visual v, double? duration = null)
    {
        var o = v.GetCached(
            $"__ST{(duration ?? DefaultOffsetDuration)}",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, STARTING_VALUE)
                    .AddKeyFrame(1, FINAL_VALUE, CubicBezierPoints.FluentDecelerate)
                    .SetDuration(duration ?? DefaultOffsetDuration)
        );

        v.Properties.SetImplicitAnimation(TRANSLATION, o);
        return v;
    }

    public static void SetDropInOut(
        FrameworkElement background,
        IList<FrameworkElement> children,
        FrameworkElement? container = null
    )
    {
        if (background is null || children.Count == 0)
        {
            return;
        }

        var delay = 0.15;

        var bv = background.EnableTranslation(true).GetElementVisual()!;
        var ease = bv.Compositor.GetCachedFluentEntranceEase();

        var bt = bv.CreateVector3KeyFrameAnimation(TRANSLATION)
            .AddKeyFrame(0, "Vector3(0, -this.Target.Size.Y, 0)")
            .AddKeyFrame(1, Vector3.Zero, ease)
            .SetInitialValueBeforeDelay()
            .SetDelayTime(delay)
            .SetDuration(0.7);

        background.SetShowAnimation(bt);

        delay += 0.15;

        foreach (var child in children)
        {
            var v = child.EnableTranslation(true).GetElementVisual()!;
            var t = v.CreateVector3KeyFrameAnimation(TRANSLATION)
                .AddKeyFrame(0, "Vector3(0, -this.Target.Size.Y, 0)")
                .AddKeyFrame(1, Vector3.Zero, ease)
                .SetInitialValueBeforeDelay()
                .SetDelayTime(delay)
                .SetDuration(0.7);

            child.SetShowAnimation(t);
            delay += 0.075;
        }

        if (container is not null)
        {
            var c = container.GetElementVisual()!;
            var clip = c.Compositor.CreateInsetClip();
            c.Clip = clip;
        }

        // Create hide animation
        List<FrameworkElement> list = [background];
        list.AddRange(children);

        var ht = bv
            .Compositor.CreateVector3KeyFrameAnimation()
            .SetTarget(TRANSLATION)
            .AddKeyFrame(1, "Vector3(0, -this.Target.Size.Y, 0)", ease)
            .SetDuration(0.5);

        foreach (var child in list)
        {
            child.SetHideAnimation(ht);
        }
    }

    public static void SetStandardFadeInOut(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement e)
        {
            SetFadeInOut(e, 200);
        }
    }

    private static void SetFadeInOut(FrameworkElement e, int durationMs)
    {
        var v = e.GetElementVisual()!;
        e.SetHideAnimation(CreateFade(v.Compositor, 0, null, durationMs));
        e.SetShowAnimation(CreateFade(v.Compositor, 1, null, durationMs));
    }

    public static void PlayFluentStartupAnimation(FrameworkElement bar, FrameworkElement content)
    {
        var bv = bar.EnableTranslation(true).GetElementVisual()!;
        bv.StartAnimation(
            bv.CreateScalarKeyFrameAnimation(nameof(Visual.Opacity))
                .AddKeyFrame(0, 0)
                .AddKeyFrame(1, 1)
                .SetDelay(0.1, AnimationDelayBehavior.SetInitialValueBeforeDelay)
                .SetDuration(0.3)
        );

        bv.StartAnimation(
            bv.CreateVector3KeyFrameAnimation(TRANSLATION)
                .AddKeyFrame(0, new Vector3(-200, 0, 0))
                .AddKeyFrame(1, Vector3.Zero, bv.Compositor.GetCachedEntranceEase())
                .SetDelay(0.1, AnimationDelayBehavior.SetInitialValueBeforeDelay)
                .SetDuration(1.2)
        );

        var cv = content.EnableCompositionTranslation().GetElementVisual()!;
        cv.StartAnimation(
            cv.CreateScalarKeyFrameAnimation(nameof(Visual.Opacity))
                .AddKeyFrame(0, 0)
                .AddKeyFrame(1, 1)
                .SetDelay(0.1, AnimationDelayBehavior.SetInitialValueBeforeDelay)
                .SetDuration(0.3)
        );

        cv.StartAnimation(
            cv.CreateVector3KeyFrameAnimation(TRANSLATION)
                .AddKeyFrame(0, new Vector3(0, 140, 0))
                .AddKeyFrame(1, Vector3.Zero, cv.Compositor.GetCachedEntranceEase())
                .SetDelay(0.1, AnimationDelayBehavior.SetInitialValueBeforeDelay)
                .SetDuration(1.2)
        );
    }

    public static void PlayStartUpAnimation(
        List<FrameworkElement> barElements,
        List<UIElement> contentElements
    )
    {
        var duration1 = TimeSpan.FromSeconds(0.7);

        var c = barElements[0].GetElementVisual()!.Compositor;
        var backOut = c.CreateEase(0.2f, 0.885f, 0.25f, 1.125f);

        var delay = 0.1;
        foreach (var element in barElements)
        {
            var v = element.EnableTranslation(true).GetElementVisual()!;
            v.StartAnimationGroup(
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, 0, -100)
                    .AddKeyFrame(1, 0, backOut)
                    .SetInitialValueBeforeDelay()
                    .SetDelayTime(TimeSpan.FromSeconds(delay))
                    .SetDuration(duration1)
            );

            delay += 0.055;
        }

        PlayEntrance(contentElements, 200);
    }

    public static void SetThemeShadow(UIElement target, float depth, params UIElement[] recievers)
    {
        try
        {
            target.Translation = new Vector3(0, 0, depth);

            ThemeShadow shadow = new();
            target.Shadow = shadow;
            foreach (var r in recievers)
            {
                shadow.Receivers.Add(r);
            }
        }
        catch { }
    }

    public static void PlayFullHeightSlideUpEntrance(FrameworkElement target)
    {
        var v = target.EnableTranslation(true).GetElementVisual()!;
        var t = v.GetCached(
            "_FHSU",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, "Vector3(0, this.Target.Size.Y, 0)")
                    .AddKeyFrame(1, "Vector3(0, 0, 0)")
                    .SetDuration(DefaultOffsetDuration)
        );

        v.StartAnimationGroup(t);
    }

    public static Vector3KeyFrameAnimation CreateSlideOut(UIElement e, float x, float y)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;
        return v.GetCached(
            "_SLDO",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, STARTING_VALUE)
                    .AddKeyFrame(1, x, y, 0)
                    .SetDuration(DefaultOffsetDuration)
        );
    }

    public static Vector3KeyFrameAnimation CreateSlideOutX(UIElement e)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;
        return v.GetCached(
            "SOX",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, STARTING_VALUE)
                    .AddKeyFrame(1, "Vector3(this.Target.Size.X, 0, 0)")
                    .SetDuration(DefaultOffsetDuration)
        );
    }

    public static Vector3KeyFrameAnimation CreateSlideOutY(UIElement e)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;
        return v.GetCached(
            "SOY",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(0, STARTING_VALUE)
                    .AddKeyFrame(1, "Vector3(0, this.Target.Size.Y, 0)")
                    .SetDuration(DefaultOffsetDuration)
        );
    }

    public static Vector3KeyFrameAnimation CreateSlideIn(UIElement e)
    {
        var v = e.EnableTranslation(true).GetElementVisual()!;
        return v.GetCached(
            "_SLDI",
            () =>
                v.CreateVector3KeyFrameAnimation(TRANSLATION)
                    .AddKeyFrame(1, Vector3.Zero)
                    .SetDuration(DefaultOffsetDuration)
        );
    }
}
