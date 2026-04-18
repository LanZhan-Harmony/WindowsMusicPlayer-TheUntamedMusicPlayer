using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace UntamedMusicPlayer.Helpers.Animations;

/// <summary>
/// Helpful extensions methods to enable you to write fluent Composition animations
/// </summary>
public static class Composition
{
    #region Fundamentals

    private static readonly Dictionary<Compositor, Dictionary<string, object>> _objCache = [];

    extension(Compositor c)
    {
        public T GetCached<T>(string key, Func<T> create)
            where T : notnull
        {
            if (!_objCache.TryGetValue(c, out var dic))
            {
                _objCache[c] = dic = [];
            }

            if (!dic.TryGetValue(key, out var value) || value is null)
            {
                dic[key] = value = create();
            }

            return (T)value;
        }
    }

    extension(CompositionObject c)
    {
        /// <summary>
        /// Gets a cached version of a CompositionObject per compositor
        /// (Each CoreWindow has it's own compositor). Allows sharing of animations
        /// without recreating everytime.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c"></param>
        /// <param name="key"></param>
        /// <param name="create"></param>
        /// <returns></returns>
        public T GetCached<T>(string key, Func<T> create)
            where T : CompositionObject
        {
            return GetCached(c.Compositor, key, create);
        }
    }

    extension(Compositor c)
    {
        public CubicBezierEasingFunction GetCachedEntranceEase()
        {
            return c.GetCached("EntranceEase", c.CreateEntranceEasingFunction);
        }

        public CubicBezierEasingFunction GetCachedFluentEntranceEase()
        {
            return c.GetCached("FluentEntranceEase", c.CreateFluentEntranceEasingFunction);
        }
    }

    #endregion

    #region Element / Base Extensions

    extension(UIElement? element)
    {
        /// <summary>
        /// Returns the Composition Hand-off Visual for this framework element
        /// </summary>
        /// <param name="element"></param>
        /// <returns>Composition Hand-off Visual</returns>
        public Visual? GetElementVisual() =>
            element is null ? null : ElementCompositionPreview.GetElementVisual(element);
    }

    extension(ScrollViewer scrollViewer)
    {
        public CompositionPropertySet GetScrollManipulationPropertySet()
        {
            return ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
        }
    }

    extension(UIElement element)
    {
        public void SetShowAnimation(ICompositionAnimationBase animation)
        {
            ElementCompositionPreview.SetImplicitShowAnimation(element, animation);
        }

        public void SetHideAnimation(ICompositionAnimationBase animation)
        {
            ElementCompositionPreview.SetImplicitHideAnimation(element, animation);
        }
    }

    extension<T>(T element)
        where T : UIElement
    {
        public T SetChildVisual(Visual visual)
        {
            ElementCompositionPreview.SetElementChildVisual(element, visual);
            return element;
        }
    }

    public static bool SupportsAlphaMask(UIElement element)
    {
        return element switch
        {
            TextBlock _ or Shape _ or Image _ => true,
            _ => false,
        };
    }

    public static InsetClip ClipToBounds(UIElement element)
    {
        var v = GetElementVisual(element);
        var c = v!.Compositor.CreateInsetClip();
        v.Clip = c;
        return c;
    }

    /// <summary>
    /// Attempts to get the AlphaMask from supported UI elements.
    /// Returns null if the element cannot create an AlphaMask.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static CompositionBrush? GetAlphaMask(UIElement element)
    {
        return element switch
        {
            TextBlock t => t.GetAlphaMask(),
            Shape s => s.GetAlphaMask(),
            Image i => i.GetAlphaMask(),
            _ => null,
        };
    }

    #endregion

    #region Translation

    extension(UIElement element)
    {
        public UIElement EnableCompositionTranslation()
        {
            return EnableCompositionTranslation(element, null);
        }

        public UIElement EnableCompositionTranslation(float x, float y, float z)
        {
            return EnableCompositionTranslation(element, new Vector3(x, y, z));
        }

        public UIElement EnableCompositionTranslation(Vector3? defaultTranslation)
        {
            var visual = GetElementVisual(element)!;
            if (
                visual.Properties.TryGetVector3(CompositionFactory.TRANSLATION, out _)
                == CompositionGetValueStatus.NotFound
            )
            {
                ElementCompositionPreview.SetIsTranslationEnabled(element, true);
                if (defaultTranslation.HasValue)
                {
                    visual.Properties.InsertVector3(
                        CompositionFactory.TRANSLATION,
                        defaultTranslation.Value
                    );
                }
                else
                {
                    visual.Properties.InsertVector3(CompositionFactory.TRANSLATION, new Vector3());
                }
            }

            return element;
        }

        public bool IsTranslationEnabled()
        {
            return GetElementVisual(element)!
                    .Properties.TryGetVector3(CompositionFactory.TRANSLATION, out _)
                != CompositionGetValueStatus.NotFound;
        }
    }

    extension(Visual visual)
    {
        public Vector3 GetTranslation()
        {
            visual.Properties.TryGetVector3(CompositionFactory.TRANSLATION, out var translation);
            return translation;
        }

        public Visual SetTranslation(float x, float y, float z)
        {
            return SetTranslation(visual, new Vector3(x, y, z));
        }

        public Visual SetTranslation(Vector3 translation)
        {
            visual.Properties.InsertVector3(CompositionFactory.TRANSLATION, translation);
            return visual;
        }

        /// <summary>
        /// Sets the axis to rotate the visual around.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public Visual SetRotationAxis(Vector3 axis)
        {
            visual.RotationAxis = axis;
            return visual;
        }

        /// <summary>
        /// Sets the axis to rotate the visual around.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Visual SetRotationAxis(float x, float y, float z)
        {
            visual.RotationAxis = new Vector3(x, y, z);
            return visual;
        }

        public Visual SetCenterPoint(float x, float y, float z)
        {
            return SetCenterPoint(visual, new Vector3(x, y, z));
        }

        public Visual SetCenterPoint(Vector3 vector)
        {
            visual.CenterPoint = vector;
            return visual;
        }

        /// <summary>
        /// Sets the centre point of a visual to its current cartesian centre (relative 0.5f, 0.5f).
        /// </summary>
        /// <param name="visual"></param>
        /// <returns></returns>
        public Visual SetCenterPoint()
        {
            return SetCenterPoint(visual, new Vector3(visual.Size / 2f, 0f));
        }
    }

    #endregion

    #region SetTarget

    extension<T>(T animation)
        where T : CompositionAnimation
    {
        public T SetTarget(string target)
        {
            animation.Target = target;
            return animation;
        }

        private T SetSafeTarget(string? target)
        {
            if (!string.IsNullOrEmpty(target))
            {
                animation.Target = target;
            }
            return animation;
        }
    }

    #endregion

    #region SetDelayTime

    extension<T>(T animation)
        where T : KeyFrameAnimation
    {
        /// <summary>
        /// Sets the delay time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="animation"></param>
        /// <param name="delayTime">Delay Time in seconds</param>
        /// <returns></returns>
        public T SetDelayTime(double delayTime)
        {
            animation.DelayTime = TimeSpan.FromSeconds(delayTime);
            return animation;
        }

        public T SetDelayTime(TimeSpan delayTime)
        {
            animation.DelayTime = delayTime;
            return animation;
        }

    #endregion

        #region SetDelay

        /// <summary>
        /// Sets the delay time in seconds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="animation"></param>
        /// <param name="delayTime">Delay Time in seconds</param>
        /// <returns></returns>
        public T SetDelay(double delayTime, AnimationDelayBehavior behavior)
        {
            animation.DelayTime = TimeSpan.FromSeconds(delayTime);
            animation.DelayBehavior = behavior;
            return animation;
        }

        public T SetDelay(TimeSpan delayTime, AnimationDelayBehavior behavior)
        {
            animation.DelayBehavior = behavior;
            animation.DelayTime = delayTime;
            return animation;
        }

        #endregion

        #region SetDelayBehaviour

        public T SetDelayBehavior(AnimationDelayBehavior behavior)
        {
            animation.DelayBehavior = behavior;
            return animation;
        }

        public T SetInitialValueBeforeDelay()
        {
            animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            return animation;
        }

        #endregion

        #region SetDuration

        /// <summary>
        /// Sets the duration in seconds. If less than 0 the duration is not set.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="duration">Duration in seconds</param>
        /// <returns></returns>
        public T SetDuration(double duration)
        {
            if (duration >= 0)
            {
                return SetDuration(animation, TimeSpan.FromSeconds(duration));
            }
            else
            {
                return animation;
            }
        }

        public T SetDuration(TimeSpan duration)
        {
            animation.Duration = duration;
            return animation;
        }

        #endregion

        #region StopBehaviour

        public T SetStopBehavior(AnimationStopBehavior stopBehavior)
        {
            animation.StopBehavior = stopBehavior;
            return animation;
        }

        #endregion

        #region Direction

        public T SetDirection(AnimationDirection direction)
        {
            animation.Direction = direction;
            return animation;
        }

        #endregion

        #region IterationBehavior

        public T SetIterationBehavior(AnimationIterationBehavior iterationBehavior)
        {
            animation.IterationBehavior = iterationBehavior;
            return animation;
        }
    }

        #endregion

    #region Comment

    extension<T>(T obj)
        where T : CompositionObject
    {
        public T SetComment(string comment)
        {
            obj.Comment = comment;
            return obj;
        }
    }

    #endregion

    #region AddKeyFrame Builders

    extension<T>(T animation)
        where T : Vector3NaturalMotionAnimation
    {
        public T SetFinalValue(Vector3 finalValue)
        {
            animation.FinalValue = finalValue;
            return animation;
        }

        public T SetFinalValue(float x, float y, float z)
        {
            animation.FinalValue = new Vector3(x, y, z);
            return animation;
        }
    }

    extension<T>(T animation)
        where T : KeyFrameAnimation
    {
        public T AddKeyFrame(float normalizedProgressKey, string expression, KeySpline spline)
        {
            animation.InsertExpressionKeyFrame(
                normalizedProgressKey,
                expression,
                animation.Compositor.CreateCubicBezierEasingFunction(spline)
            );
            return animation;
        }

        public T AddKeyFrame(
            float normalizedProgressKey,
            string expression,
            CubicBezierPoints spline
        )
        {
            animation.InsertExpressionKeyFrame(
                normalizedProgressKey,
                expression,
                animation.Compositor.CreateCubicBezierEasingFunction(spline)
            );
            return animation;
        }

        public T AddKeyFrame(
            float normalizedProgressKey,
            string expression,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertExpressionKeyFrame(normalizedProgressKey, expression, ease);
            return animation;
        }
    }

    extension(ScalarKeyFrameAnimation animation)
    {
        public ScalarKeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }

        public ScalarKeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                value,
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }
    }

    extension(ColorKeyFrameAnimation animation)
    {
        public ColorKeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Color value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }
    }

    extension(Vector2KeyFrameAnimation animation)
    {
        public Vector2KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector2 value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }

        public Vector2KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector2 value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                value,
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }
    }

    #region Vector3

    extension(Vector3KeyFrameAnimation animation)
    {
        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector3 value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }

        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector3 value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                value,
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }

        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector3 value,
            CubicBezierPoints ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                value,
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }

        /// <summary>
        /// Adds a Vector3KeyFrame where the X & Y components are set to the input value and the Z component defaults to 0f.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="normalizedProgressKey"></param>
        /// <param name="value"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, new Vector3(value, value, 0f), ease);
            return animation;
        }

        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                new Vector3(value, value, 0f),
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }

        /// <summary>
        /// Adds a Vector3KeyFrame where the X & Y components are set to the input value and the Z component defaults to 1f.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="normalizedProgressKey"></param>
        /// <param name="value"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public Vector3KeyFrameAnimation AddScaleKeyFrame(
            float normalizedProgressKey,
            float value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, new Vector3(value, value, 1f), ease);
            return animation;
        }

        /// <summary>
        /// Adds a Vector3KeyFrame where the X & Y components are set to the input value and the Z component defaults to 1f.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="normalizedProgressKey"></param>
        /// <param name="value"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public Vector3KeyFrameAnimation AddScaleKeyFrame(
            float normalizedProgressKey,
            float value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                new Vector3(value, value, 1f),
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }

        /// <summary>
        /// Adds a Vector3KeyFrame using the X & Y components. The Z component defaults to 0f.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="normalizedProgressKey"></param>
        /// <param name="x">X Component of the Vector3</param>
        /// <param name="y">Y Component of the Vector3</param>
        /// <param name="ease">Optional ease</param>
        /// <returns></returns>
        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float x,
            float y,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, new Vector3(x, y, 0f), ease);
            return animation;
        }

        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float x,
            float y,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                new Vector3(x, y, 0f),
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }

        /// <summary>
        /// Adds a Vector3KeyFrame with X Y & Z components.
        /// </summary>
        /// <param name="animation"></param>
        /// <param name="normalizedProgressKey"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public Vector3KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            float x,
            float y,
            float z,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, new Vector3(x, y, z), ease);
            return animation;
        }
    }

    #endregion

    extension(Vector4KeyFrameAnimation animation)
    {
        public Vector4KeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Vector4 value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }
    }

    extension(QuaternionKeyFrameAnimation animation)
    {
        public QuaternionKeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Quaternion value,
            CompositionEasingFunction? ease = null
        )
        {
            animation.InsertKeyFrame(normalizedProgressKey, value, ease);
            return animation;
        }

        public QuaternionKeyFrameAnimation AddKeyFrame(
            float normalizedProgressKey,
            Quaternion value,
            KeySpline ease
        )
        {
            animation.InsertKeyFrame(
                normalizedProgressKey,
                value,
                animation.Compositor.CreateCubicBezierEasingFunction(ease)
            );
            return animation;
        }
    }

    #endregion

    #region Compositor Create Builders

    private static T TryAddGroup<T>(CompositionObject obj, T animation)
        where T : CompositionAnimation
    {
        if (obj is CompositionAnimationGroup group)
        {
            group.Add(animation);
        }

        return animation;
    }

    extension(CompositionObject visual)
    {
        public SpringVector3NaturalMotionAnimation CreateSpringVector3Animation(
            string? targetProperty = null
        )
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateSpringVector3Animation().SetSafeTarget(targetProperty)
            );
        }

        public ColorKeyFrameAnimation CreateColorKeyFrameAnimation(string? targetProperty = null)
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateColorKeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public ScalarKeyFrameAnimation CreateScalarKeyFrameAnimation(string? targetProperty = null)
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateScalarKeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public Vector2KeyFrameAnimation CreateVector2KeyFrameAnimation(
            string? targetProperty = null
        )
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateVector2KeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public Vector3KeyFrameAnimation CreateVector3KeyFrameAnimation(
            string? targetProperty = null
        )
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateVector3KeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public Vector4KeyFrameAnimation CreateVector4KeyFrameAnimation(
            string? targetProperty = null
        )
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateVector4KeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public QuaternionKeyFrameAnimation CreateQuaternionKeyFrameAnimation(
            string? targetProperty = null
        )
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateQuaternionKeyFrameAnimation().SetSafeTarget(targetProperty)
            );
        }

        public ExpressionAnimation CreateExpressionAnimation()
        {
            return TryAddGroup(visual, visual.Compositor.CreateExpressionAnimation());
        }

        public ExpressionAnimation CreateExpressionAnimation(string targetProperty)
        {
            return TryAddGroup(
                visual,
                visual.Compositor.CreateExpressionAnimation().SetTarget(targetProperty)
            );
        }
    }

    extension(Compositor compositor)
    {
        public CubicBezierEasingFunction CreateCubicBezierEasingFunction(
            float x1,
            float y1,
            float x2,
            float y2
        )
        {
            return compositor.CreateCubicBezierEasingFunction(
                new Vector2(x1, y1),
                new Vector2(x2, y2)
            );
        }

        public CubicBezierEasingFunction CreateCubicBezierEasingFunction(KeySpline spline)
        {
            return compositor.CreateCubicBezierEasingFunction(
                spline.ControlPoint1.ToVector2(),
                spline.ControlPoint2.ToVector2()
            );
        }

        public CubicBezierEasingFunction CreateCubicBezierEasingFunction(CubicBezierPoints points)
        {
            return compositor.CreateCubicBezierEasingFunction(points.Start, points.End);
        }
    }

    #endregion

    #region SetExpression

    extension(ExpressionAnimation animation)
    {
        public ExpressionAnimation SetExpression(string expression)
        {
            animation.Expression = expression;
            return animation;
        }
    }

    #endregion

    #region SetParameter Builders

    extension<T>(T animation)
        where T : CompositionAnimation
    {
        public T SetParameter(string key, UIElement parameter)
        {
            if (parameter is not null)
            {
                animation.SetReferenceParameter(key, GetElementVisual(parameter));
            }

            return animation;
        }

        public T SetParameter(string key, CompositionObject parameter)
        {
            animation.SetReferenceParameter(key, parameter);
            return animation;
        }

        public T SetParameter(string key, float parameter)
        {
            animation.SetScalarParameter(key, parameter);
            return animation;
        }

        public T SetParameter(string key, double parameter)
        {
            animation.SetScalarParameter(key, (float)parameter);
            return animation;
        }

        public T SetParameter(string key, Vector2 parameter)
        {
            animation.SetVector2Parameter(key, parameter);
            return animation;
        }

        public T SetParameter(string key, Vector3 parameter)
        {
            animation.SetVector3Parameter(key, parameter);
            return animation;
        }

        public T SetParameter(string key, Vector4 parameter)
        {
            animation.SetVector4Parameter(key, parameter);
            return animation;
        }
    }

    #endregion

    #region PropertySet Builders

    extension(CompositionPropertySet set)
    {
        public CompositionPropertySet Insert(string name, float value)
        {
            set.InsertScalar(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, bool value)
        {
            set.InsertBoolean(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Vector2 value)
        {
            set.InsertVector2(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Vector3 value)
        {
            set.InsertVector3(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Vector4 value)
        {
            set.InsertVector4(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Color value)
        {
            set.InsertColor(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Matrix3x2 value)
        {
            set.InsertMatrix3x2(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Matrix4x4 value)
        {
            set.InsertMatrix4x4(name, value);
            return set;
        }

        public CompositionPropertySet Insert(string name, Quaternion value)
        {
            set.InsertQuaternion(name, value);
            return set;
        }
    }

    #endregion

    #region Animation Start / Stop

    extension(CompositionObject compositionObject)
    {
        public void StartAnimation(CompositionAnimation animation)
        {
            if (string.IsNullOrWhiteSpace(animation.Target))
            {
                throw new ArgumentNullException("Animation has no target");
            }

            compositionObject.StartAnimation(animation.Target, animation);
        }

        public void StartAnimation(CompositionAnimationGroup animation)
        {
            compositionObject.StartAnimationGroup(animation);
        }

        public void StopAnimation(CompositionAnimation animation)
        {
            if (string.IsNullOrWhiteSpace(animation.Target))
            {
                throw new ArgumentNullException("Animation has no target");
            }

            compositionObject.StopAnimation(animation.Target);
        }
    }

    #endregion

    #region Brushes

    extension(LinearGradientBrush brush)
    {
        public CompositionGradientBrush AsCompositionBrush(Compositor compositor)
        {
            var compBrush = compositor.CreateLinearGradientBrush();

            foreach (var stop in brush.GradientStops)
            {
                compBrush.ColorStops.Add(
                    compositor.CreateColorGradientStop((float)stop.Offset, stop.Color)
                );
            }

            // todo : try and copy transforms?

            return compBrush;
        }
    }

    #endregion

    #region Extras

    extension(Compositor c)
    {
        public CubicBezierEasingFunction CreateEase(float x1, float y1, float x2, float y2)
        {
            return c.CreateCubicBezierEasingFunction(new(x1, y1), new(x2, y2));
        }

        public CubicBezierEasingFunction CreateEntranceEasingFunction()
        {
            return c.CreateCubicBezierEasingFunction(new(.1f, .9f), new(.2f, 1));
        }

        public CubicBezierEasingFunction CreateFluentEntranceEasingFunction()
        {
            return c.CreateCubicBezierEasingFunction(new(0f, 0f), new(0f, 1));
        }

        public CompositionAnimationGroup CreateAnimationGroup(
            params CompositionAnimation[] animations
        )
        {
            var group = c.CreateAnimationGroup();
            foreach (var a in animations)
            {
                group.Add(a);
            }

            return group;
        }
    }

    extension<T>(T c)
        where T : CompositionObject
    {
        public bool HasImplicitAnimation(string path)
        {
            return c.ImplicitAnimations is not null
                && c.ImplicitAnimations.TryGetValue(path, out var v)
                && v is not null;
        }

        public T SetImplicitAnimation(string path, ICompositionAnimationBase? animation = null)
        {
            if (c.ImplicitAnimations is null)
            {
                if (animation is null)
                {
                    return c;
                }

                c.ImplicitAnimations = c.Compositor.CreateImplicitAnimationCollection();
            }

            if (animation is null)
            {
                c.ImplicitAnimations.Remove(path);
            }
            else
            {
                c.ImplicitAnimations[path] = animation;
            }

            return c;
        }
    }

    extension(FrameworkElement element)
    {
        public FrameworkElement SetImplicitAnimation(
            string path,
            ICompositionAnimationBase? animation = null
        )
        {
            SetImplicitAnimation(GetElementVisual(element)!, path, animation);
            return element;
        }
    }

    #endregion
}
