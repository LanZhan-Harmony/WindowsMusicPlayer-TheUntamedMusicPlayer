using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;
using Windows.Foundation;
using Windows.UI;

namespace UntamedMusicPlayer.Helpers.Animations;

public enum AnimationTargetType
{
    Undefined = 0,
    RootContainer = 1,
    Child = 2,
}

/// <summary>
/// A collection of useful animation-focused extension methods
/// </summary>
public static class Animation
{
    #region Composite Transform

    extension(FrameworkElement element)
    {
        public CompositeTransform GetNewCompositeTransform(
            bool centreOriginOnCreation = true,
            bool overwriteOtherTransforms = true
        )
        {
            element.RenderTransform = null;
            return element.GetCompositeTransform(centreOriginOnCreation, overwriteOtherTransforms);
        }

        public CompositeTransform GetCompositeTransform(
            bool centreOriginOnCreation = true,
            bool overwriteOtherTransforms = true
        )
        {
            var ct = element.RenderTransform as CompositeTransform;
            if (ct is not null)
            {
                return ct;
            }

            // 3. If there's nothing there, create a new CompositeTransform
            if (element.RenderTransform is null)
            {
                element.RenderTransform = new CompositeTransform();
                ct = (CompositeTransform)element.RenderTransform;
                if (centreOriginOnCreation)
                {
                    ct.CenterX = ct.CenterY = 0.5;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
            }
            else
            {
                ct = new CompositeTransform();
                if (centreOriginOnCreation)
                {
                    ct.CenterX = ct.CenterY = 0.5;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                // 5. See if the existing item is a singular transform, and convert it to a CompositeTransform
                if (element.RenderTransform is Transform transform)
                {
                    ApplyTransform(ref ct, transform);
                    element.RenderTransform = ct;
                }
                else
                {
                    // 6. If we're a group of transforms, convert each child individually
                    if (element.RenderTransform is TransformGroup group)
                    {
                        foreach (var tran in group.Children)
                        {
                            ApplyTransform(ref ct, tran);
                        }

                        element.RenderTransform = ct;
                    }
                }
            }
            return ct;
        }
    }

    /// <summary>
    /// Adds the effect of a regular transform to a composite transform
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="t"></param>
    internal static void ApplyTransform(ref CompositeTransform ct, Transform t)
    {
        if (t is TranslateTransform tt)
        {
            ct.TranslateX = tt.X;
            ct.TranslateY = tt.Y;
        }
        else if (t is RotateTransform rt)
        {
            ct.Rotation = rt.Angle;
            ct.CenterX = rt.CenterX;
            ct.CenterY = rt.CenterY;
        }
        else if (t is SkewTransform sK)
        {
            ct.SkewX = sK.AngleX;
            ct.SkewY = sK.AngleY;
            ct.CenterX = sK.CenterX;
            ct.CenterY = sK.CenterY;
        }
        else if (t is ScaleTransform sc)
        {
            ct.ScaleX = sc.ScaleX;
            ct.ScaleY = sc.ScaleY;
            ct.CenterX = sc.CenterX;
            ct.CenterY = sc.CenterY;
        }
    }

    #endregion

    #region Plane Projection

    extension(FrameworkElement element)
    {
        /// <summary>
        /// Gets the plane projection from a FrameworkElement's projection property. If
        /// the property is null or not a plane projection, a new plane projection is created
        /// and set as the plane projection and then returned
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public PlaneProjection GetPlaneProjection()
        {
            if (element.Projection is not PlaneProjection projection)
            {
                element.Projection = new PlaneProjection();
                projection = (PlaneProjection)element.Projection;
            }
            return projection;
        }
    }

    #endregion

    #region Storyboard

    extension(Storyboard? storyboard)
    {
        /// <summary>
        /// Returns an await-able task that runs the storyboard through to completion
        /// </summary>
        /// <param name="storyboard"></param>
        /// <returns></returns>
        public Task BeginAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            if (storyboard is null)
            {
                tcs.SetException(new ArgumentNullException(nameof(storyboard)));
            }
            else
            {
                void onComplete(object? s, object e)
                {
                    storyboard.Completed -= onComplete;
                    tcs.SetResult(true);
                }

                storyboard.Completed += onComplete;
                storyboard.Begin();
            }
            return tcs.Task;
        }

        public void AddTimeline(Timeline timeline, DependencyObject target, string targetProperty)
        {
            if (target is FrameworkElement frameworkElement)
            {
                if (targetProperty.StartsWith(TargetProperty.CompositeTransform.Identifier))
                {
                    GetCompositeTransform(frameworkElement);
                }
                else if (targetProperty.StartsWith(TargetProperty.PlaneProjection.Identifier))
                {
                    GetPlaneProjection(frameworkElement);
                }
                else if (targetProperty.StartsWith(TargetProperty.CompositeTransform3D.Identifier))
                {
                    GetCompositeTransform3D(frameworkElement);
                }
            }

            Storyboard.SetTarget(timeline, target);
            Storyboard.SetTargetProperty(timeline, targetProperty);

            storyboard?.Children.Add(timeline);
        }
    }

    #endregion

    #region Timelines

    extension(DoubleAnimationUsingKeyFrames doubleAnimation)
    {
        public DoubleAnimationUsingKeyFrames AddDiscreteKeyFrame(double seconds, double value)
        {
            doubleAnimation.AddDiscreteKeyFrame(TimeSpan.FromSeconds(seconds), value);
            return doubleAnimation;
        }

        public DoubleAnimationUsingKeyFrames AddDiscreteKeyFrame(TimeSpan time, double value)
        {
            doubleAnimation.KeyFrames.Add(
                new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(time), Value = value }
            );
            return doubleAnimation;
        }

        /// <summary>
        /// Adds a <see cref="LinearDoubleKeyFrame"/>
        /// </summary>
        /// <param name="doubleAnimation"></param>
        /// <param name="seconds">Duration in seconds</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(double seconds, double value)
        {
            doubleAnimation.AddLinearDoubleKeyFrame(TimeSpan.FromSeconds(seconds), value);
            return doubleAnimation;
        }

        /// <summary>
        /// Adds a <see cref="LinearDoubleKeyFrame"/>
        /// </summary>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(TimeSpan keyTime, double value)
        {
            doubleAnimation.AddLinearDoubleKeyFrame(keyTime, value);
            return doubleAnimation;
        }

        /// <summary>
        /// Adds a <see cref="SplineDoubleKeyFrame"/>
        /// </summary>
        /// <param name="doubleAnimation"></param>
        /// <param name="seconds">Duration in seconds</param>
        /// <param name="value"></param>
        /// <param name="spline"></param>
        /// <returns></returns>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(
            double seconds,
            double value,
            KeySpline spline
        )
        {
            doubleAnimation.AddSplineDoubleKeyFrame(TimeSpan.FromSeconds(seconds), value, spline);
            return doubleAnimation;
        }

        /// <summary>
        /// Adds a <see cref="SplineDoubleKeyFrame"/>
        /// </summary>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(
            TimeSpan keyTime,
            double value,
            KeySpline spline
        )
        {
            doubleAnimation.AddSplineDoubleKeyFrame(keyTime, value, spline);
            return doubleAnimation;
        }

        /// <summary>
        /// Adds an <see cref="EasingDoubleKeyFrame"/>
        /// </summary>
        /// <param name="doubleAnimation"></param>
        /// <param name="seconds">Duration in seconds</param>
        /// <param name="value"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(
            double seconds,
            double value,
            EasingFunctionBase? ease = null
        )
        {
            doubleAnimation.AddEasingDoubleKeyFrame(TimeSpan.FromSeconds(seconds), value, ease);
            return doubleAnimation;
        }

        /// <summary>
        /// Adds an <see cref="EasingDoubleKeyFrame"/>
        /// </summary>
        public DoubleAnimationUsingKeyFrames AddKeyFrame(
            TimeSpan keyTime,
            double value,
            EasingFunctionBase? ease = null
        )
        {
            doubleAnimation.AddEasingDoubleKeyFrame(keyTime, value, ease);
            return doubleAnimation;
        }

        private DoubleAnimationUsingKeyFrames AddLinearDoubleKeyFrame(TimeSpan time, double value)
        {
            doubleAnimation.KeyFrames.Add(
                new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(time), Value = value }
            );

            return doubleAnimation;
        }

        private DoubleAnimationUsingKeyFrames AddEasingDoubleKeyFrame(
            TimeSpan time,
            double value,
            EasingFunctionBase? ease = null
        )
        {
            doubleAnimation.KeyFrames.Add(
                new EasingDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(time),
                    Value = value,
                    EasingFunction = ease,
                }
            );

            return doubleAnimation;
        }

        private DoubleAnimationUsingKeyFrames AddSplineDoubleKeyFrame(
            TimeSpan time,
            double value,
            KeySpline? spline = null
        )
        {
            doubleAnimation.KeyFrames.Add(
                new SplineDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(time),
                    Value = value,
                    KeySpline = spline?.Clone(),
                }
            );
            return doubleAnimation;
        }
    }

    extension(ObjectAnimationUsingKeyFrames objectAnimation)
    {
        public ObjectAnimationUsingKeyFrames AddKeyFrame(double seconds, object value)
        {
            objectAnimation.AddKeyFrame(TimeSpan.FromSeconds(seconds), value);
            return objectAnimation;
        }

        public ObjectAnimationUsingKeyFrames AddKeyFrame(TimeSpan time, object value)
        {
            objectAnimation.KeyFrames.Add(
                new DiscreteObjectKeyFrame { KeyTime = KeyTime.FromTimeSpan(time), Value = value }
            );
            return objectAnimation;
        }
    }

    extension<T>(T t)
        where T : Timeline
    {
        public T If(bool condition, Action<T> action)
        {
            if (condition)
            {
                action(t);
            }

            return t;
        }
    }

    extension(ColorAnimationUsingKeyFrames colorAnimation)
    {
        public ColorAnimationUsingKeyFrames AddKeyFrame(double seconds, Color color)
        {
            colorAnimation.AddKeyFrame(TimeSpan.FromSeconds(seconds), color);
            return colorAnimation;
        }

        public ColorAnimationUsingKeyFrames AddKeyFrame(TimeSpan time, Color color)
        {
            colorAnimation.KeyFrames.Add(
                new EasingColorKeyFrame { KeyTime = KeyTime.FromTimeSpan(time), Value = color }
            );
            return colorAnimation;
        }
    }

    #endregion

    #region Fluency

    extension(DoubleAnimation animation)
    {
        public DoubleAnimation SetEase(EasingFunctionBase ease)
        {
            animation.EasingFunction = ease;
            return animation;
        }

        public DoubleAnimation To(double? value)
        {
            animation.To = value;
            return animation;
        }

        public DoubleAnimation By(double? value)
        {
            animation.By = value;
            return animation;
        }

        public DoubleAnimation From(double? value)
        {
            animation.From = value;
            return animation;
        }

        public DoubleAnimation Easing(EasingFunctionBase ease)
        {
            animation.EasingFunction = ease;
            return animation;
        }
    }

    extension<T>(T storyboard)
        where T : Timeline
    {
        public T SetSpeedRatio(double speedRatio)
        {
            storyboard.SpeedRatio = speedRatio;
            return storyboard;
        }

        /// <summary>
        /// Sets the BeginTime property on a Storyboard
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storyboard"></param>
        /// <param name="beginTime">Begin time in Seconds</param>
        /// <returns></returns>
        public T SetBeginTime(double beginTime)
        {
            return SetBeginTime(storyboard, TimeSpan.FromSeconds(beginTime));
        }

        public T SetBeginTime(TimeSpan beginTime)
        {
            storyboard.BeginTime = beginTime;
            return storyboard;
        }

        /// <summary>
        /// Sets the Duration of a Timeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storyboard"></param>
        /// <param name="duration">Duration in seconds</param>
        /// <returns></returns>
        public T SetDuration(double duration)
        {
            return SetDuration(storyboard, TimeSpan.FromSeconds(duration));
        }

        public T SetDuration(TimeSpan duration)
        {
            storyboard.Duration = duration;
            return storyboard;
        }

        public T SetRepeatBehavior(RepeatBehavior behavior)
        {
            storyboard.RepeatBehavior = behavior;
            return storyboard;
        }
    }

    extension(DoubleAnimation storyboard)
    {
        public DoubleAnimation EnableDependentAnimation(bool value)
        {
            storyboard.EnableDependentAnimation = value;
            return storyboard;
        }
    }

    extension(DoubleAnimationUsingKeyFrames storyboard)
    {
        public DoubleAnimationUsingKeyFrames EnableDependentAnimations(bool value)
        {
            storyboard.EnableDependentAnimation = value;
            return storyboard;
        }
    }

    public static T CreateTimeline<T>(
        DependencyObject target,
        string targetProperty,
        Storyboard parent
    )
        where T : Timeline, new()
    {
        var timeline = new T();
        parent.AddTimeline(timeline, target, targetProperty);
        return timeline;
    }

    extension(Storyboard parent)
    {
        public T CreateTimeline<T>(DependencyObject target, string targetProperty)
            where T : Timeline, new()
        {
            var timeline = new T();
            parent.AddTimeline(timeline, target, targetProperty);
            return timeline;
        }
    }

    extension(Storyboard storyboard)
    {
        public Storyboard Build(Action<Storyboard> action)
        {
            action(storyboard);
            return storyboard;
        }
    }

    #endregion

    #region Transform3D

    extension(FrameworkElement target)
    {
        public CompositeTransform3D GetCompositeTransform3D()
        {
            if (target.Transform3D is not CompositeTransform3D transform)
            {
                transform = new CompositeTransform3D();
                target.Transform3D = transform;
            }

            return transform;
        }

        public PerspectiveTransform3D GetPerspectiveTransform3D()
        {
            if (target.Transform3D is not PerspectiveTransform3D transform)
            {
                target.Transform3D = transform = new PerspectiveTransform3D();
            }

            return transform;
        }
    }

    #endregion

    #region KeySplines


    public static KeySpline CreateKeySpline(double x1, double y1, double x2, double y2)
    {
        var keyspline = new KeySpline();
        keyspline.SetPoints(x1, y1, x2, y2);
        return keyspline;
    }

    extension(KeySpline keySpline)
    {
        public KeySpline Reverse()
        {
            return new KeySpline
            {
                ControlPoint1 = new Point(keySpline.ControlPoint1.Y, keySpline.ControlPoint1.X),
                ControlPoint2 = new Point(keySpline.ControlPoint2.Y, keySpline.ControlPoint2.X),
            };
        }

        public KeySpline Clone()
        {
            return new KeySpline
            {
                ControlPoint1 = keySpline.ControlPoint1,
                ControlPoint2 = keySpline.ControlPoint2,
            };
        }

        public void SetPoints(double x1, double y1, double x2, double y2)
        {
            keySpline.ControlPoint1 = new Point(x1, y1);
            keySpline.ControlPoint2 = new Point(x2, y2);
        }
    }

    #endregion
}

/// <summary>
/// A collection of common PropertyPath's used by XAML Storyboards
/// for creating independently animate-able animations. Hence the word
/// "independent". Don't add CPU bound properties here please. I don't
/// care for them.
/// </summary>
public static class TargetProperty
{
    //------------------------------------------------------
    //
    // UI Element
    //
    //------------------------------------------------------

    // UIElement Opacity
    public const string Opacity = "(UIElement.Opacity)";

    // UIElement Visibility
    public const string Visibility = "(UIElement.Visibility)";

    // UIElement IsHitTestVisible
    public const string IsHitTestVisible = "(UIElement.IsHitTestVisible)";

    // Grid ColumnSpan
    public const string GridColumnSpan = "(Grid.ColumnSpan)";

    // Grid RowSpan
    public const string GridRowSpan = "(Grid.RowSpan)";

    //------------------------------------------------------
    //
    // Composite Transform (Render Transform)
    //
    //------------------------------------------------------

    public class CompositeTransform
    {
        public const string Identifier = "(UIElement.RenderTransform).(CompositeTransform.";

        // Render Transform Composite Transform X-Axis Translation
        public const string TranslateX =
            "(UIElement.RenderTransform).(CompositeTransform.TranslateX)";

        // Render Transform Composite Transform Y-Axis Translation
        public const string TranslateY =
            "(UIElement.RenderTransform).(CompositeTransform.TranslateY)";

        // Render Transform Composite Transform X-Axis Scale
        public const string ScaleX = "(UIElement.RenderTransform).(CompositeTransform.ScaleX)";

        // Render Transform Composite Transform Y-Axis Scale
        public const string ScaleY = "(UIElement.RenderTransform).(CompositeTransform.ScaleY)";

        // Render Transform Composite Transform X-Scale Skew
        public const string SkewX = "(UIElement.RenderTransform).(CompositeTransform.SkewX)";

        // Render Transform Composite Transform Y-Scale Skew
        public const string SkewY = "(UIElement.RenderTransform).(CompositeTransform.SkewY)";

        // Render Transform Composite Transform Rotation
        public const string Rotation = "(UIElement.RenderTransform).(CompositeTransform.Rotation)";
    }

    //------------------------------------------------------
    //
    //  Plane Projection
    //
    //------------------------------------------------------

    public class PlaneProjection
    {
        public const string Identifier = "(UIElement.Projection).(PlaneProjection.";

        // Plane Projection X-Axis Rotation
        public const string RotationX = "(UIElement.Projection).(PlaneProjection.RotationX)";

        // Plane Projection Y-Axis Rotation
        public const string RotationY = "(UIElement.Projection).(PlaneProjection.RotationY)";

        // Plane Projection Z-Axis Rotation
        public const string RotationZ = "(UIElement.Projection).(PlaneProjection.RotationZ)";

        public const string GlobalOffsetX =
            "(UIElement.Projection).(PlaneProjection.GlobalOffsetX)";
        public const string GlobalOffsetY =
            "(UIElement.Projection).(PlaneProjection.GlobalOffsetY)";
        public const string GlobalOffsetZ =
            "(UIElement.Projection).(PlaneProjection.GlobalOffsetZ)";

        public const string LocalOffsetX = "(UIElement.Projection).(PlaneProjection.LocalOffsetX)";
        public const string LocalOffsetY = "(UIElement.Projection).(PlaneProjection.LocalOffsetY)";
        public const string LocalOffsetZ = "(UIElement.Projection).(PlaneProjection.LocalOffsetZ)";

        public const string CenterOfRotationX =
            "(UIElement.Projection).(PlaneProjection.CenterOfRotationX)";
        public const string CenterOfRotationY =
            "(UIElement.Projection).(PlaneProjection.CenterOfRotationY)";
        public const string CenterOfRotationZ =
            "(UIElement.Projection).(PlaneProjection.CenterOfRotationZ)";
    }

    //------------------------------------------------------
    //
    //  Composite Transform 3D (Transform 3D)
    //
    //------------------------------------------------------

    public class CompositeTransform3D
    {
        public const string Identifier = "(UIElement.Transform3D).(CompositeTransform3D.";

        public const string TranslateX =
            "(UIElement.Transform3D).(CompositeTransform3D.TranslateX)";
        public const string TranslateY =
            "(UIElement.Transform3D).(CompositeTransform3D.TranslateY)";
        public const string TranslateZ =
            "(UIElement.Transform3D).(CompositeTransform3D.TranslateZ)";

        public const string RotationX = "(UIElement.Transform3D).(CompositeTransform3D.RotationX)";
        public const string RotationY = "(UIElement.Transform3D).(CompositeTransform3D.RotationY)";
        public const string RotationZ = "(UIElement.Transform3D).(CompositeTransform3D.RotationZ)";

        public const string ScaleX = "(UIElement.Transform3D).(CompositeTransform3D.ScaleX)";
        public const string ScaleY = "(UIElement.Transform3D).(CompositeTransform3D.ScaleY)";
        public const string ScaleZ = "(UIElement.Transform3D).(CompositeTransform3D.ScaleZ)";

        public const string CenterX = "(UIElement.Transform3D).(CompositeTransform3D.CenterX)";
        public const string CenterY = "(UIElement.Transform3D).(CompositeTransform3D.CenterY)";
        public const string CenterZ = "(UIElement.Transform3D).(CompositeTransform3D.CenterZ)";
    }
}

public class CubicBezierPoints(float x1, float y1, float x2, float y2)
{
    public Vector2 Start { get; } = new(x1, y1);
    public Vector2 End { get; } = new(x2, y2);

    //------------------------------------------------------
    //
    //  Fluent Splines
    //
    //------------------------------------------------------

    /*
        These splines are taken from Microsoft's official animation documentation for
        fluent animation design system.

        For reference recommended durations are:
            Exit Animations         : 150ms
            Entrance Animations     : 300ms
            Translation Animations  : <= 500ms
    */

    /// <summary>
    /// Analogous to Exponential EaseIn, Exponent 4.5
    /// </summary>
    public static CubicBezierPoints FluentAccelerate => new(0.07f, 0, 1, 0.5f);

    /// <summary>
    /// Analogous to Exponential EaseOut, Exponent 7
    /// </summary>
    public static CubicBezierPoints FluentDecelerate => new(0.1f, 0.9f, 0.2f, 1);

    /// <summary>
    /// Analogous to Circle EaseInOut
    /// </summary>
    public static CubicBezierPoints FluentStandard => new(0.8f, 0, 0.2f, 1);

    public static CubicBezierPoints FluentEntrance => new(0, 0, 0, 1f);
}

public static class KeySplines
{
    public static KeySpline Create(CubicBezierPoints point)
    {
        KeySpline spline = new()
        {
            ControlPoint1 = point.Start.ToPoint(),
            ControlPoint2 = point.End.ToPoint(),
        };
        return spline;
    }

    /// <summary>
    /// Returns a KeySpline for use as an easing function
    /// to replicate the easing of the EntranceThemeTransition
    /// </summary>
    /// <returns></returns>
    public static KeySpline EntranceTheme => Animation.CreateKeySpline(0.1, 0.9, 0.2, 1);

    /// <summary>
    /// A KeySpline that closely matches the default easing curve applied to
    /// Composition animations by Windows when the developer does not specify
    /// any easing function.
    /// </summary>
    public static KeySpline CompositionDefault =>
        Animation.CreateKeySpline(0.395, 0.56, 0.06, 0.95);

    /// <summary>
    /// Intended for 500 millisecond opacity animation for depth animations
    /// </summary>
    public static KeySpline DepthZoomOpacity => Animation.CreateKeySpline(0.2, 0.6, 0.3, 0.9);

    /// <summary>
    /// A more precise alternative to EntranceTheme KeySpline
    /// </summary>
    public static KeySpline Popup =>
        Animation.CreateKeySpline(0.100000001490116, 0.899999976158142, 0.200000002980232, 1);

    //------------------------------------------------------
    //
    //  Fluent KeySplines
    //
    //------------------------------------------------------

    /*
        These splines are taken from Microsoft's official animation documentation for
        fluent animation design system.

        For reference recommended durations are:
            Exit Animations         : 150ms
            Entrance Animations     : 300ms
            Translation Animations  : <= 500ms
    */

    /// <summary>
    /// Analogous to Exponential EaseIn, Exponent 4.5
    /// </summary>
    public static KeySpline FluentAccelerate => Create(CubicBezierPoints.FluentAccelerate);

    /// <summary>
    /// Analogous to Exponential EaseOut, Exponent 7
    /// </summary>
    public static KeySpline FluentDecelerate => Create(CubicBezierPoints.FluentDecelerate);

    /// <summary>
    /// Analogous to Circle EaseInOut
    /// </summary>
    public static KeySpline FluentStandard => Create(CubicBezierPoints.FluentStandard);

    public static KeySpline FluentEntrance => Create(CubicBezierPoints.FluentEntrance);

    //------------------------------------------------------
    //
    //  Standard Penner Splines
    //
    //------------------------------------------------------

    public static KeySpline CubicInOut => Animation.CreateKeySpline(0.645, 0.045, 0.355, 1);

    public static KeySpline QuinticInOut => Animation.CreateKeySpline(0.86, 0, 0.07, 1);
}
