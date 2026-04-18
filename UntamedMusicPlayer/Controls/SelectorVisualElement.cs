using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Helpers.Animations;
using Windows.Foundation;
using Windows.UI;

namespace UntamedMusicPlayer.Controls;

public class CompositionTransition : DependencyObject
{
    public bool IsValid => Duration.HasTimeSpan && KeySpline is not null;

    public Duration Duration
    {
        get => (Duration)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(Duration),
        typeof(CompositionTransition),
        new PropertyMetadata(default(Duration))
    );

    public KeySpline KeySpline
    {
        get => (KeySpline)GetValue(KeySplineProperty);
        set => SetValue(KeySplineProperty, value);
    }

    public static readonly DependencyProperty KeySplineProperty = DependencyProperty.Register(
        nameof(KeySpline),
        typeof(KeySpline),
        typeof(CompositionTransition),
        new PropertyMetadata(default(KeySpline))
    );

    public CompositionEasingFunction GetEase(Compositor c)
    {
        return Composition.GetCached<CompositionEasingFunction>(
            c,
            $"__ctEa{KeySpline}",
            () =>
            {
                return c.CreateCubicBezierEasingFunction(KeySpline);
            }
        );
    }
}

public enum SelectionVisualType
{
    PointerOver, // Should render
    Selection, // Should render on top of ListViewBase internal ScrollContentPresenter
    Focus, // Should be rendered on top of Window.Current.Content
}

public enum SelectorInteractionState
{
    None,
    PointerOver,
}

public partial class SelectorVisualElement : FrameworkElement
{
    private ShapeVisual? _containerShapes;
    private ShapeVisual? _barShapes;
    private ContainerVisual? _container;
    private CompositionRoundedRectangleGeometry? _rect;
    private CompositionRoundedRectangleGeometry? _bar;
    private CompositionColorBrush? _fillBrush;
    private CompositionColorBrush? _strokeBrush;
    private CompositionColorBrush? _barFillBrush;
    private readonly CompositionPropertySet _props;
    private CompositionSpriteShape? _barSprite;

    private ExpressionAnimation? _exp;

    private const string barXY_horizontal =
        "Vector2("
        + "((Container.Size.X - Bar.Size.X) / 2) + 2,"
        + "Container.Size.Y - Bar.Size.Y - props.Padding)";

    private const string barXY_vertical =
        "Vector2(" + "props.Padding, " + "((Container.Size.Y - Bar.Size.Y) / 2) + 2)";

    private readonly List<(DependencyProperty, long)> _tokens = [];

    public SelectorVisualElement()
    {
        IsHitTestVisible = false;
        Loaded += SelectorVisual_Loaded;
        Unloaded += SelectorVisual_Unloaded;
        _props = this.GetElementVisual()!.Compositor.CreatePropertySet();
    }

    public static SelectorVisualElement GetElement(DependencyObject obj) =>
        (SelectorVisualElement)obj.GetValue(ElementProperty);

    public static void SetElement(DependencyObject obj, SelectorVisualElement value) =>
        obj.SetValue(ElementProperty, value);

    public static readonly DependencyProperty ElementProperty = DependencyProperty.RegisterAttached(
        "Element",
        typeof(SelectorVisualElement),
        typeof(SelectorVisualElement),
        new PropertyMetadata(default(SelectorVisualElement), (d, e) => OnElementChanged(d, e))
    );

    public static DataTemplate GetElementTemplate(DependencyObject obj) =>
        (DataTemplate)obj.GetValue(ElementTemplateProperty);

    public static void SetElementTemplate(DependencyObject obj, DataTemplate value) =>
        obj.SetValue(ElementTemplateProperty, value);

    public static readonly DependencyProperty ElementTemplateProperty =
        DependencyProperty.RegisterAttached(
            "ElementTemplate",
            typeof(DataTemplate),
            typeof(SelectorVisualElement),
            new PropertyMetadata(default(DataTemplate), (d, e) => OnElementTemplateChanged(d, e))
        );

    public Point VisualCornerRadius
    {
        get => (Point)GetValue(VisualCornerRadiusProperty);
        set => SetValue(VisualCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty VisualCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(VisualCornerRadius),
            typeof(Point),
            typeof(SelectorVisualElement),
            new PropertyMetadata(
                default(Point),
                (d, e) =>
                {
                    if (d is SelectorVisualElement o)
                    {
                        o.OnVisualCornerRadiusChanged(
                            (Point)(e.OldValue ?? (Point)default),
                            (Point)(e.NewValue ?? (Point)default)
                        );
                    }
                }
            )
        );

    public Point VisualOffset
    {
        get => (Point)GetValue(VisualOffsetProperty);
        set => SetValue(VisualOffsetProperty, value);
    }

    public static readonly DependencyProperty VisualOffsetProperty = DependencyProperty.Register(
        nameof(VisualOffset),
        typeof(Point),
        typeof(SelectorVisualElement),
        new PropertyMetadata(default(Point))
    );

    public HorizontalAlignment VisualHorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(VisualHorizontalAlignmentProperty);
        set => SetValue(VisualHorizontalAlignmentProperty, value);
    }

    public static readonly DependencyProperty VisualHorizontalAlignmentProperty =
        DependencyProperty.Register(
            nameof(VisualHorizontalAlignment),
            typeof(HorizontalAlignment),
            typeof(SelectorVisualElement),
            new PropertyMetadata(
                HorizontalAlignment.Stretch,
                (d, e) =>
                {
                    if (d is SelectorVisualElement o)
                    {
                        o.Update();
                    }
                }
            )
        );

    public VerticalAlignment VisualVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(VisualVerticalAlignmentProperty);
        set => SetValue(VisualVerticalAlignmentProperty, value);
    }

    public static readonly DependencyProperty VisualVerticalAlignmentProperty =
        DependencyProperty.Register(
            nameof(VisualVerticalAlignment),
            typeof(VerticalAlignment),
            typeof(SelectorVisualElement),
            new PropertyMetadata(
                VerticalAlignment.Stretch,
                (d, e) =>
                {
                    if (d is SelectorVisualElement o)
                    {
                        o.Update();
                    }
                }
            )
        );

    public Thickness VisualInset
    {
        get => (Thickness)GetValue(VisualInsetProperty);
        set => SetValue(VisualInsetProperty, value);
    }

    public static readonly DependencyProperty VisualInsetProperty = DependencyProperty.Register(
        nameof(VisualInset),
        typeof(Thickness),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(Thickness),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.Update();
                }
            }
        )
    );

    public CompositionTransition SizeTransition
    {
        get => (CompositionTransition)GetValue(SizeTransitionProperty);
        set => SetValue(SizeTransitionProperty, value);
    }

    public static readonly DependencyProperty SizeTransitionProperty = DependencyProperty.Register(
        nameof(SizeTransition),
        typeof(CompositionTransition),
        typeof(SelectorVisualElement),
        new PropertyMetadata(default(CompositionTransition))
    );

    public CompositionTransition CornerTransition
    {
        get => (CompositionTransition)GetValue(CornerTransitionProperty);
        set => SetValue(CornerTransitionProperty, value);
    }

    public static readonly DependencyProperty CornerTransitionProperty =
        DependencyProperty.Register(
            nameof(CornerTransition),
            typeof(CompositionTransition),
            typeof(SelectorVisualElement),
            new PropertyMetadata(default(CompositionTransition))
        );

    public CompositionTransition OffsetTransition
    {
        get => (CompositionTransition)GetValue(OffsetTransitionProperty);
        set => SetValue(OffsetTransitionProperty, value);
    }

    public static readonly DependencyProperty OffsetTransitionProperty =
        DependencyProperty.Register(
            nameof(OffsetTransition),
            typeof(CompositionTransition),
            typeof(SelectorVisualElement),
            new PropertyMetadata(default(CompositionTransition))
        );

    public SolidColorBrush Fill
    {
        get => (SolidColorBrush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill),
        typeof(SolidColorBrush),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(SolidColorBrush),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnFillChanged(e.OldValue as SolidColorBrush, e.NewValue as SolidColorBrush);
                }
            }
        )
    );

    public SolidColorBrush Stroke
    {
        get => (SolidColorBrush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke),
        typeof(SolidColorBrush),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(SolidColorBrush),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnStrokeChanged(e.OldValue as SolidColorBrush, e.NewValue as SolidColorBrush);
                }
            }
        )
    );

    public SolidColorBrush BarFill
    {
        get => (SolidColorBrush)GetValue(BarFillProperty);
        set => SetValue(BarFillProperty, value);
    }

    public static readonly DependencyProperty BarFillProperty = DependencyProperty.Register(
        nameof(BarFill),
        typeof(SolidColorBrush),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(SolidColorBrush),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnBarFillChanged(
                        e.OldValue as SolidColorBrush,
                        e.NewValue as SolidColorBrush
                    );
                }
            }
        )
    );

    public SelectionVisualType Mode
    {
        get => (SelectionVisualType)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode),
        typeof(SelectionVisualType),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(SelectionVisualType),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnModeChanged(
                        (SelectionVisualType)(e.OldValue ?? (SelectionVisualType)default),
                        (SelectionVisualType)(e.NewValue ?? (SelectionVisualType)default)
                    );
                }
            }
        )
    );

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(double),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnStrokeThicknessChanged(
                        (double)(e.OldValue ?? (double)default),
                        (double)(e.NewValue ?? (double)default)
                    );
                }
            }
        )
    );

    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target),
        typeof(FrameworkElement),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(FrameworkElement),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnTargetChanged(
                        e.OldValue as FrameworkElement,
                        e.NewValue as FrameworkElement
                    );
                }
            }
        )
    );

    public FrameworkElement? DisplayTarget
    {
        get => (FrameworkElement?)GetValue(DisplayTargetProperty);
        set => SetValue(DisplayTargetProperty, value);
    }

    public static readonly DependencyProperty DisplayTargetProperty = DependencyProperty.Register(
        nameof(DisplayTarget),
        typeof(FrameworkElement),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(FrameworkElement),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnDisplayTargetChanged(
                        e.OldValue as FrameworkElement,
                        e.NewValue as FrameworkElement
                    );
                }
            }
        )
    );

    public Point BarSize
    {
        get => (Point)GetValue(BarSizeProperty);
        set => SetValue(BarSizeProperty, value);
    }

    public static readonly DependencyProperty BarSizeProperty = DependencyProperty.Register(
        nameof(BarSize),
        typeof(Point),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(Point),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnBarSizeChanged(
                        (Point)(e.OldValue ?? (Point)default),
                        (Point)(e.NewValue ?? (Point)default)
                    );
                }
            }
        )
    );

    public double BarPadding
    {
        get => (double)GetValue(BarPaddingProperty);
        set => SetValue(BarPaddingProperty, value);
    }

    public static readonly DependencyProperty BarPaddingProperty = DependencyProperty.Register(
        nameof(BarPadding),
        typeof(double),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            default(double),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnBarPaddingChanged(
                        (double)(e.OldValue ?? (double)default),
                        (double)(e.NewValue ?? (double)default)
                    );
                }
            }
        )
    );

    public double VisualSizeAdjustment
    {
        get => (double)GetValue(VisualSizeAdjustmentProperty);
        set => SetValue(VisualSizeAdjustmentProperty, value);
    }

    public static readonly DependencyProperty VisualSizeAdjustmentProperty =
        DependencyProperty.Register(
            nameof(VisualSizeAdjustment),
            typeof(double),
            typeof(SelectorVisualElement),
            new PropertyMetadata(default(double))
        );

    public Point BarCornerRadius
    {
        get => (Point)GetValue(BarCornerRadiusProperty);
        set => SetValue(BarCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty BarCornerRadiusProperty = DependencyProperty.Register(
        nameof(BarCornerRadius),
        typeof(Point),
        typeof(SelectorVisualElement),
        new PropertyMetadata(
            new Point(2, 2),
            (d, e) =>
            {
                if (d is SelectorVisualElement o)
                {
                    o.OnBarCornerRadiusChanged(
                        (Point)(e.OldValue ?? (Point)default),
                        (Point)(e.NewValue ?? (Point)default)
                    );
                }
            }
        )
    );

    public bool RightCornerRadiusOnly
    {
        get => (bool)GetValue(RightCornerRadiusOnlyProperty);
        set => SetValue(RightCornerRadiusOnlyProperty, value);
    }

    public static readonly DependencyProperty RightCornerRadiusOnlyProperty =
        DependencyProperty.Register(
            nameof(RightCornerRadiusOnly),
            typeof(bool),
            typeof(SelectorVisualElement),
            new PropertyMetadata(
                default(bool),
                (d, e) =>
                {
                    if (d is SelectorVisualElement o)
                    {
                        o.Update();
                    }
                }
            )
        );

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(SelectorVisualElement),
        new PropertyMetadata(Orientation.Horizontal)
    );

    public bool UseMaterialCornerRadius
    {
        get => (bool)GetValue(UseMaterialCornerRadiusProperty);
        set => SetValue(UseMaterialCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty UseMaterialCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(UseMaterialCornerRadius),
            typeof(bool),
            typeof(SelectorVisualElement),
            new PropertyMetadata(
                default(bool),
                (d, e) =>
                {
                    if (d is SelectorVisualElement o)
                    {
                        o.OnUseMaterialCornerRadiusChanged(
                            (bool)(e.OldValue ?? (bool)default),
                            (bool)(e.NewValue ?? (bool)default)
                        );
                    }
                }
            )
        );

    private void OnUseMaterialCornerRadiusChanged(bool o, bool n)
    {
        if (n is false)
        {
            _rect?.StopAnimation(nameof(_rect.CornerRadius));
        }

        SetCornerAnimation();

        Update();
    }

    private void SetCornerAnimation()
    {
        if (_rect is null)
        {
            return;
        }

        if (UseMaterialCornerRadius is false)
        {
            _rect.CornerRadius = VisualCornerRadius.ToVector2();
        }
        else
        {
            _rect.StartAnimation(
                _rect
                    .CreateExpressionAnimation(nameof(_rect.CornerRadius))
                    .SetExpression("Vector2(this.Target.Size.Y/2f, this.Target.Size.Y/2f)")
            );
        }
    }

    private void Update()
    {
        if (VisualTreeHelper.GetParent(this) is not FrameworkElement)
        {
            return;
        }

        CreateVisual();
        SetCornerAnimation();
    }

    private static Color GetColor(SolidColorBrush? b)
    {
        if (b is null)
        {
            return Colors.Transparent;
        }

        return b.Color with
        {
            A = (byte)(b.Color.A * b.Opacity),
        };
    }

    private void OnFillChanged(SolidColorBrush? o, SolidColorBrush? n)
    {
        // Note: This implementation does not support updating the brush properties.
        _fillBrush?.Color = GetColor(n);
    }

    private void OnStrokeChanged(SolidColorBrush? o, SolidColorBrush? n)
    {
        // Note: This implementation does not support updating the brush properties.
        _strokeBrush?.Color = GetColor(n);
    }

    private void OnBarFillChanged(SolidColorBrush? o, SolidColorBrush? n)
    {
        _barFillBrush?.Color = GetColor(n);
    }

    private void OnStrokeThicknessChanged(double o, double n)
    {
        if (
            _containerShapes is not null
            && _containerShapes.Shapes.FirstOrDefault() is CompositionSpriteShape s
        )
        {
            s.StrokeThickness = (float)n;
        }
    }

    private void OnBarSizeChanged(Point o, Point n)
    {
        if (_bar is not null && _barShapes is not null)
        {
            _bar.Size = n.ToVector2();
            _barShapes.Size = _bar.Size;
            _props.InsertVector2("BarSize", _bar.Size);
        }
    }

    private void OnBarPaddingChanged(double o, double n)
    {
        _props?.InsertScalar("Padding", (float)BarPadding);
    }

    private void OnBarCornerRadiusChanged(Point o, Point n)
    {
        _bar?.CornerRadius = n.ToVector2();
    }

    private void OnVisualCornerRadiusChanged(Point o, Point n)
    {
        if (_rect is not null)
        {
            SetCornerAnimation();
        }
    }

    private void CreateVisual()
    {
        if (_containerShapes is not null)
        {
            return;
        }

        var c = this.GetElementVisual()!.Compositor;

        _fillBrush = c.CreateColorBrush(GetColor(Fill));
        _strokeBrush = c.CreateColorBrush(GetColor(Stroke));
        _barFillBrush = c.CreateColorBrush(GetColor(BarFill));

        _rect = c.CreateRoundedRectangleGeometry();
        _rect.CornerRadius = VisualCornerRadius.ToVector2();

        // Create background shape
        var s = c.CreateSpriteShape(_rect);

        s.StrokeBrush = _strokeBrush;
        s.StrokeThickness = (float)StrokeThickness;
        s.FillBrush = _fillBrush;

        _containerShapes = c.CreateShapeVisual();
        _containerShapes.Shapes.Add(s);

        _container = c.CreateContainerVisual();
        _container.Children.InsertAtTop(_containerShapes);

        // Create bar shape
        _bar = c.CreateRoundedRectangleGeometry();
        _bar.CornerRadius = BarCornerRadius.ToVector2();
        _bar.Size = BarSize.ToVector2();
        var bs = c.CreateSpriteShape(_bar);
        bs.FillBrush = _barFillBrush;
        bs.CenterPoint = _bar.Size / 2;
        _barSprite = bs;

        _props.Insert("Padding", (float)BarPadding);

        // Setup bar shape
        _barShapes = c.CreateShapeVisual();

        CompositionFactory.StartCentering(_barShapes);
        _barShapes.Shapes.Add(bs);
        _container.Children.InsertAtTop(_barShapes);

        // Bar scale
        _barShapes.SetImplicitAnimation(
            "Scale",
            bs.CreateVector3KeyFrameAnimation("Scale")
                .AddKeyFrame(1, "this.FinalValue")
                .SetDuration(0.3)
        );

        var str = Orientation is Orientation.Horizontal ? barXY_horizontal : barXY_vertical;

        // Position bar
        _exp = bs.CreateExpressionAnimation("Offset.XY")
            .SetExpression(str)
            .SetParameter("Container", _rect)
            .SetParameter("Bar", _bar)
            .SetParameter("props", _props);

        bs.StartAnimation(_exp);

        SetCornerAnimation();

        if (DisplayTarget is null)
        {
            this.SetChildVisual(_container);
        }
        else
        {
            DisplayTarget.SetChildVisual(_container);
        }
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }

    public void MoveTo(FrameworkElement? target, FrameworkElement container, bool show = true)
    {
        if (target is null)
        {
            return;
        }

        var animate = true;

        CreateVisual();

        if (
            _containerShapes is null
            || _barShapes is null
            || _container is null
            || _rect is null
            || _bar is null
        )
        {
            return;
        }

        var containerShapes = _containerShapes;
        var barShapes = _barShapes;
        var containerVisual = _container;
        var rect = _rect;
        var bar = _bar;

        if (show && Visibility == Visibility.Collapsed)
        {
            Visibility = Visibility.Visible;
            animate = false;
        }

        // TODO: listen to new target's SizeChanged event,
        //       and unhook previous target

        // 1: Get the target element's position relative to the container
        var r = target.GetBoundingRect(container);
        var position = new Vector3(
            (float)r.Left + (float)VisualInset.Left,
            (float)r.Top + (float)VisualInset.Top,
            0
        );

        position += new Vector3(VisualOffset.ToVector2(), 0);

        // 2: Get the target element's size
        var size = target.ActualSize;
        size -= new Point(
            VisualInset.Left + VisualInset.Right,
            VisualInset.Top + VisualInset.Bottom
        ).ToVector2();
        size += new Vector2(-(float)VisualSizeAdjustment, 0);

        var size2 = size += new Vector2((float)StrokeThickness);

        if (size.X < 0 || size.Y < 0)
        {
            size = new();
        }

        position += new Vector3((float)VisualSizeAdjustment, 0, 0);

        // 3: Animation position
        SetOffset(animate);
        containerShapes.Offset = position;
        barShapes.Offset = position;

        // 4: Animation size
        SetSizeAnimation(containerShapes, animate);
        SetSizeAnimation(barShapes, animate);
        SetSizeAnimation(containerVisual, animate);
        SetSizeAnimation(rect, animate);
        SetSizeAnimation(bar, animate);

        containerShapes.Size = size2;
        barShapes.Size = size2;
        containerVisual.Size = size2;

        if (size.LengthSquared() > VisualCornerRadius.ToVector2().LengthSquared())
        {
            rect.Size = size - VisualCornerRadius.ToVector2();
            rect.Offset = VisualCornerRadius.ToVector2() / 2f;
        }
    }

    private void SetOffset(bool animate)
    {
        if (_containerShapes is null || _barShapes is null)
        {
            return;
        }

        var containerShapes = _containerShapes;
        var barShapes = _barShapes;

        if (animate && OffsetTransition is { IsValid: true })
        {
            if (containerShapes.HasImplicitAnimation("Offset"))
            {
                return;
            }

            var ani = containerShapes
                .CreateVector3KeyFrameAnimation("Offset")
                .AddKeyFrame(
                    1,
                    "this.FinalValue",
                    OffsetTransition.GetEase(containerShapes.Compositor)
                )
                .SetDuration(OffsetTransition.Duration.TimeSpan.TotalSeconds);

            containerShapes.SetImplicitAnimation("Offset", ani);
            barShapes.SetImplicitAnimation("Offset", ani);
        }
        else
        {
            containerShapes.SetImplicitAnimation("Offset", null);
            barShapes.SetImplicitAnimation("Offset", null);
        }
    }

    private void SetSizeAnimation(CompositionObject o, bool animate)
    {
        if (animate && SizeTransition is { IsValid: true })
        {
            if (o.HasImplicitAnimation("Size"))
            {
                return;
            }

            o.SetImplicitAnimation(
                "Size",
                o.CreateVector2KeyFrameAnimation("Size")
                    .AddKeyFrame(1, "this.FinalValue", SizeTransition.GetEase(o.Compositor))
                    .SetDuration(SizeTransition.Duration.TimeSpan.TotalSeconds)
            );
        }
        else
        {
            o.SetImplicitAnimation("Size", null);
        }
    }

    //------------------------------------------------------
    //
    // XAML Lifecycle
    //
    //------------------------------------------------------

    #region Lifecycle

    private void SelectorVisual_Loaded(object sender, RoutedEventArgs e)
    {
        Register();
        Update();
    }

    private void SelectorVisual_Unloaded(object sender, RoutedEventArgs e)
    {
        Unregister();

        static void Dispose<T>(ref T? d)
            where T : class, IDisposable
        {
            d?.Dispose();
            d = null;
        }

        this.SetChildVisual(null!);

        Dispose(ref _container);
        Dispose(ref _containerShapes);
        Dispose(ref _barShapes);
        Dispose(ref _rect);
        Dispose(ref _bar);
        Dispose(ref _fillBrush);
        Dispose(ref _strokeBrush);
        Dispose(ref _barFillBrush);
        Dispose(ref _barSprite);
    }

    private void Register()
    {
        Unregister();

        Do(HorizontalAlignmentProperty);
        Do(VerticalAlignmentProperty);
        Do(MarginProperty);

        void Do(DependencyProperty p)
        {
            _tokens.Add((p, RegisterPropertyChangedCallback(p, Callback)));
        }
    }

    private void Unregister()
    {
        foreach (var token in _tokens)
        {
            UnregisterPropertyChangedCallback(token.Item1, token.Item2);
        }
    }

    private void Callback(DependencyObject sender, DependencyProperty dp)
    {
        Update();
    }

    #endregion


    //------------------------------------------------------
    //
    // Attached Properties
    //
    //------------------------------------------------------

    private void OnDisplayTargetChanged(FrameworkElement? o, FrameworkElement? n)
    {
        if (o is FrameworkElement old)
        {
            old.SetChildVisual(null!);
        }

        if (n is FrameworkElement newValue)
        {
            this.SetChildVisual(null!);

            if (_container is not null)
            {
                newValue.SetChildVisual(_container);
            }
            else
            {
                newValue.SetChildVisual(null!);
            }
        }
        else
        {
            this.SetChildVisual(null!);
        }
    }

    private void OnModeChanged(SelectionVisualType o, SelectionVisualType n)
    {
        OnTargetChanged(Target, Target);
    }

    private void OnTargetChanged(FrameworkElement? o, FrameworkElement? n)
    {
        if (o is ListViewBase oldList)
        {
            OnListViewTargetChanged(oldList, null);
        }

        if (o is RadioButtons oldRadios)
        {
            OnRadioButtonsTargetChanged(oldRadios, null);
        }

        if (n is ListViewBase newList)
        {
            OnListViewTargetChanged(null, newList);
        }

        if (n is RadioButtons newRadios)
        {
            OnRadioButtonsTargetChanged(null, newRadios);
        }
    }

    private static void OnElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SelectorVisualElement oldValue)
        {
            oldValue.Target = null;
        }

        if (e.NewValue is SelectorVisualElement newValue)
        {
            newValue.Target = (FrameworkElement)d;
        }
    }

    private static void OnElementTemplateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is FrameworkElement f)
        {
            if (e.OldValue is not null && e.NewValue is null)
            {
                SetElement(f, null!);
            }

            if (e.NewValue is DataTemplate t)
            {
                SetElement(f, (SelectorVisualElement)t.LoadContent());
            }
        }
    }

    //------------------------------------------------------
    //
    // Drag Handling
    //
    //------------------------------------------------------

    internal void SetState(SelectorInteractionState state)
    {
        if (_barSprite is null || _bar is null)
        {
            return;
        }

        //_barSprite.CenterPoint = new Vector2(0.5f);
        //_barSprite.Offset = BarSize.ToVector2()/2f;

        if (state is SelectorInteractionState.PointerOver)
        {
            if (Orientation == Orientation.Horizontal)
            {
                _bar.Size = new((float)BarSize.X * 2f, (float)BarSize.Y);
            }
            else
            {
                _bar.Size = new((float)BarSize.X, (float)BarSize.Y * 1.5f);
            }
        }
        else
        {
            _bar.Size = BarSize.ToVector2();
        }
    }

    //------------------------------------------------------
    //
    // Drag Handling
    //
    //------------------------------------------------------

    public void StartMove()
    {
        SetOffset(false);
    }

    public void Move(Vector2 offset)
    {
        if (Orientation == Orientation.Horizontal)
        {
            MoveX(offset.X);
        }
        else
        {
            MoveY(offset.Y);
        }
    }

    private void MoveX(float offset)
    {
        if (_containerShapes is null || _rect is null)
        {
            return;
        }

        var containerShapes = _containerShapes;
        var rect = _rect;
        var target = containerShapes.Offset + new Vector3(offset, 0, 0);
        if (target.X < 0)
        {
            target = target with { X = 0 };
        }

        if (VisualTreeHelper.GetParent(this) is FrameworkElement parent)
        {
            var max = parent.ActualWidth - rect.Size.X;
            if (target.X > max)
            {
                target = target with { X = (float)max };
            }
        }

        containerShapes.Offset = target;
    }

    private void MoveY(float offset)
    {
        if (_containerShapes is null || _rect is null)
        {
            return;
        }

        var containerShapes = _containerShapes;
        var rect = _rect;
        var target = containerShapes.Offset + new Vector3(0, offset, 0);
        if (target.Y < 0)
        {
            target = target with { Y = 0 };
        }

        if (VisualTreeHelper.GetParent(this) is FrameworkElement parent)
        {
            var max = parent.ActualHeight - rect.Size.Y;
            if (target.Y > max)
            {
                target = target with { Y = (float)max };
            }
        }

        containerShapes.Offset = target;
    }

    public void EndMove()
    {
        SetOffset(true);
    }

    public Rect GetBounds()
    {
        if (_containerShapes is null)
        {
            return default;
        }

        return new Rect(
            _containerShapes.Offset.X,
            _containerShapes.Offset.Y,
            _containerShapes.Size.X,
            _containerShapes.Size.Y
        );
    }
}

//------------------------------------------------------
//
// RadioButtons helpers
//
//------------------------------------------------------

public partial class SelectorVisualElement // RADIOBUTTONS
{
    private void OnRadioButtonsTargetChanged(RadioButtons? o, RadioButtons? n)
    {
        if (o is RadioButtons old)
        {
            old.Loaded -= Radios_Loaded;
            old.SelectionChanged -= Radios_Selection;
            old.SizeChanged -= Radios_SizeChanged;
        }

        if (n is RadioButtons nrb)
        {
            nrb.Loaded -= Radios_Loaded;
            nrb.SizeChanged -= Radios_SizeChanged;

            if (nrb.ActualSize.X <= 0)
            {
                nrb.Loaded += Radios_Loaded;
            }
            else
            {
                nrb.SizeChanged += Radios_SizeChanged;
                RadioMoveTo(nrb, nrb.SelectedIndex);
            }

            if (nrb.GetFirstDescendantOfType<FrameworkElement>("BorderRoot") is { } b)
            {
                DisplayTarget = b;
            }

            nrb.SelectionChanged -= Radios_Selection;
            nrb.SelectionChanged += Radios_Selection;
        }

        void Radios_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (sender is RadioButtons rb)
            {
                RadioMoveTo(rb, rb.SelectedIndex);
            }
        }

        void Radios_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButtons rb)
            {
                rb.SizeChanged -= Radios_SizeChanged;
                rb.SizeChanged += Radios_SizeChanged;

                if (rb.GetFirstDescendantOfType<FrameworkElement>("BorderRoot") is { } b)
                {
                    DisplayTarget = b;
                }

                RadioMoveTo(rb, rb.SelectedIndex);
            }
        }

        void Radios_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is RadioButtons rb)
            {
                RadioMoveTo(rb, rb.SelectedIndex);
            }
        }
    }

    public void RadioMoveTo(RadioButtons b, int index)
    {
        if (VisualTreeHelper.GetParent(b) is null)
        {
            return;
        }

        if (index < 0)
        {
            // If anyone else uses this look at fixing this,
            // but Radios fires removed selection event before the added one,
            // not both at the same time. This will break our movement
            // animation if we handle it with this "basic" code.
            //this.Hide();
            return;
        }

        var items = b.GetFirstLevelDescendantsOfType<RadioButton>().ToList();
        var target = b.GetFirstLevelDescendantsOfType<RadioButton>()
            .Skip(b.SelectedIndex)
            .FirstOrDefault();
        MoveTo(target, b);
    }
}

//------------------------------------------------------
//
// ListView helpers
//
//------------------------------------------------------

public partial class SelectorVisualElement // EXTENDEDLISTVIEW
{
    private void OnListViewTargetChanged(ListViewBase? o, ListViewBase? n)
    {
        if (o is ListViewBase old)
        {
            old.Loaded -= Lvb_Loaded;
            old.SelectionChanged -= Lv_SelectionChanged;
        }

        if (n is ListViewBase lvb)
        {
            lvb.Loaded -= Lvb_Loaded;

            if (Mode == SelectionVisualType.Selection)
            {
                if (
                    lvb.GetFirstDescendantOfType<ScrollContentPresenter>(s =>
                        s.Name == "ScrollContentPresenter"
                    ) is
                    { } s
                )
                {
                    Attach(lvb, this, s);
                }
                else
                {
                    lvb.Loaded += Lvb_Loaded;
                }
            }
        }

        void Lvb_Loaded(object sender, RoutedEventArgs e)
        {
            var lv = (ListViewBase)sender;
            lv.Loaded -= Lvb_Loaded;

            if (Mode == SelectionVisualType.Selection)
            {
                if (
                    lv.GetFirstDescendantOfType<ScrollContentPresenter>(s =>
                        s.Name == "ScrollContentPresenter"
                    ) is
                    { } s
                )
                {
                    Attach(lv, GetElement(lv), s);
                }
            }
        }

        static void Attach(ListViewBase lv, SelectorVisualElement sv, ScrollContentPresenter sp)
        {
            if (sp is null)
            {
                return;
            }

            lv.SelectionChanged -= Lv_SelectionChanged;

            // Hook item changed
            lv.SelectionChanged += Lv_SelectionChanged;
            if (sp.Content is ItemsPresenter ip)
            {
                sv.DisplayTarget = ip;
            }
        }

        static void Lv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var l = (ListViewBase)sender;
            var sve = GetElement(l);

            if (e.AddedItems.FirstOrDefault() is { } item)
            {
                if (l.ContainerFromItem(item) is FrameworkElement fe)
                {
                    if (sve.DisplayTarget is FrameworkElement target)
                    {
                        sve.MoveTo(fe, target, true);
                    }
                    else
                    {
                        sve.MoveTo(fe, l, true);
                    }
                }
                else if (e.RemovedItems.Count > 0)
                {
                    sve.Hide();
                }
            }
        }
    }
}
