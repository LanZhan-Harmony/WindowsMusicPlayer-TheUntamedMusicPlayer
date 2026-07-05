using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace UntamedMusicPlayer.Controls;

/// <summary>
/// 提供类似 Reveal 聚焦效果的画板，可附加到任意 FrameworkElement 上，
/// 在其周围生成动态光晕边框和覆盖层。
/// </summary>
public partial class RevealFocusPanel : Panel
{
    // ---------- 附加属性 ----------
    public static readonly DependencyProperty AttachToPanelProperty =
        DependencyProperty.RegisterAttached(
            "AttachToPanel",
            typeof(RevealFocusPanel),
            typeof(RevealFocusPanel),
            new PropertyMetadata(null, OnAttachToPanelChanged)
        );

    public static void SetAttachToPanel(FrameworkElement element, RevealFocusPanel panel) =>
        element.SetValue(AttachToPanelProperty, panel);

    public static RevealFocusPanel GetAttachToPanel(FrameworkElement element) =>
        (RevealFocusPanel)element.GetValue(AttachToPanelProperty);

    // ---------- 私有成员 ----------
    private readonly Compositor _compositor;
    private CompositionPropertySet _globalPropertySet = null!;
    private ScalarKeyFrameAnimation _opacityForwardAnimation = null!;
    private ScalarKeyFrameAnimation _opacityBackwardAnimation = null!;
    private Vector2KeyFrameAnimation _revealBrushRadiusForwardAnimation = null!;
    private Vector2KeyFrameAnimation _revealBrushRadiusBackwardAnimation = null!;
    private ExpressionAnimation _hostVisualSizeExpressionAnimation = null!;
    private ExpressionAnimation _borderGeometrySizeExpressionAnimation = null!;
    private ExpressionAnimation _ellipseCenterExpressionAnimation = null!;
    private ExpressionAnimation _visualOffsetExpressionAnimation = null!;

    private static readonly Vector2 InitialMousePosition = new(float.MaxValue, float.MaxValue);

    private readonly Canvas _overlayCanvas = new();
    private ContainerVisual _overlayContainer = null!;

    // ---------- 构造函数 ----------
    public RevealFocusPanel()
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        Background = new SolidColorBrush(Colors.Transparent);

        // 覆盖层画布放在 Z 轴最底层，确保在其他内容后面
        Canvas.SetZIndex(_overlayCanvas, -1);
        Children.Add(_overlayCanvas);
    }

    // ---------- 资源初始化 ----------
    private void CreateResourcesIfNeeded()
    {
        if (_globalPropertySet != null)
        {
            return;
        }

        _globalPropertySet = _compositor.CreatePropertySet();
        _globalPropertySet.InsertVector2("MousePosition", InitialMousePosition);

        // 透明度动画（淡入/淡出）
        _opacityForwardAnimation = _compositor.CreateScalarKeyFrameAnimation();
        _opacityForwardAnimation.InsertKeyFrame(1f, 0.4f);

        _opacityBackwardAnimation = _compositor.CreateScalarKeyFrameAnimation();
        _opacityBackwardAnimation.InsertKeyFrame(1f, 0f);

        // 光晕半径动画（按下/释放）
        _revealBrushRadiusForwardAnimation = _compositor.CreateVector2KeyFrameAnimation();
        _revealBrushRadiusForwardAnimation.InsertKeyFrame(1f, RevealBrush.NormalRevealRadius * 3);
        _revealBrushRadiusForwardAnimation.Duration = TimeSpan.FromSeconds(3);

        _revealBrushRadiusBackwardAnimation = _compositor.CreateVector2KeyFrameAnimation();
        _revealBrushRadiusBackwardAnimation.InsertKeyFrame(1f, RevealBrush.NormalRevealRadius);

        // 表达式动画
        _hostVisualSizeExpressionAnimation = _compositor.CreateExpressionAnimation(
            "hostVisual.Size"
        );
        _borderGeometrySizeExpressionAnimation = _compositor.CreateExpressionAnimation(
            "hostVisual.Size - Vector2(strokeWidth, strokeWidth)"
        );

        _ellipseCenterExpressionAnimation = _compositor.CreateExpressionAnimation(
            "globalProperty.MousePosition - localProperty.elementPosition"
        );
        _ellipseCenterExpressionAnimation.SetReferenceParameter(
            "globalProperty",
            _globalPropertySet
        );

        _visualOffsetExpressionAnimation = _compositor.CreateExpressionAnimation(
            "localProperty.elementPosition"
        );

        // 覆盖容器（用于容纳所有附加元素的光晕）
        _overlayContainer = _compositor.CreateContainerVisual();

        // 将容器剪裁到面板边界，避免溢出（例如滚动区域）
        var panelVisual = ElementCompositionPreview.GetElementVisual(this);
        var sizeExpression = _compositor.CreateExpressionAnimation("panelVisual.Size");
        sizeExpression.SetReferenceParameter("panelVisual", panelVisual);
        _overlayContainer.StartAnimation("Size", sizeExpression);
        _overlayContainer.Clip = _compositor.CreateInsetClip();

        ElementCompositionPreview.SetElementChildVisual(_overlayCanvas, _overlayContainer);

        // 全局鼠标位置追踪
        PointerEntered += OnUpdateMousePosition;
        PointerMoved += OnUpdateMousePosition;
        PointerExited += (s, e) =>
        {
            _globalPropertySet.InsertVector2("MousePosition", InitialMousePosition);
        };
    }

    private void OnUpdateMousePosition(object sender, PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint(this).Position;
        _globalPropertySet.InsertVector2(
            "MousePosition",
            new Vector2((float)point.X, (float)point.Y)
        );
    }

    // ---------- 附加属性变更回调 ----------
    private static void OnAttachToPanelChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var panel = e.NewValue as RevealFocusPanel;
        if (panel == null)
        {
            return;
        }

        panel.CreateResourcesIfNeeded();

        var child = d as FrameworkElement;
        if (child == null)
        {
            return;
        }

        var compositor = panel._compositor;
        var elementVisual = ElementCompositionPreview.GetElementVisual(child);

        // 创建边框几何和覆盖几何
        var borderGeometry = compositor.CreateRoundedRectangleGeometry();
        var overlayGeometry = compositor.CreateRoundedRectangleGeometry();

        const float strokeThickness = 1.0f;
        var halfStroke = strokeThickness / 2.0f;

        // 边框几何尺寸（减去了描边宽度）
        panel._borderGeometrySizeExpressionAnimation.SetReferenceParameter(
            "hostVisual",
            elementVisual
        );
        panel._borderGeometrySizeExpressionAnimation.SetScalarParameter(
            "strokeWidth",
            strokeThickness
        );
        borderGeometry.StartAnimation("Size", panel._borderGeometrySizeExpressionAnimation);

        // 绑定 CornerRadius（如果目标元素支持）
        var cornerRadius = BindToCornerRadiusProperty(
            child,
            halfStroke,
            borderGeometry,
            overlayGeometry
        );
        if (cornerRadius.TopLeft != -1)
        {
            var adjustedRadius = Math.Max(0, (float)cornerRadius.TopLeft - halfStroke);
            borderGeometry.CornerRadius = new Vector2(adjustedRadius, adjustedRadius);
            overlayGeometry.CornerRadius = new Vector2(
                (float)cornerRadius.TopLeft,
                (float)cornerRadius.TopLeft
            );
        }

        borderGeometry.Offset = new Vector2(halfStroke, halfStroke);

        // 创建画刷和形状
        var brush = RevealBrush.Create(compositor);
        var borderShape = compositor.CreateSpriteShape(borderGeometry);
        borderShape.StrokeThickness = strokeThickness;
        borderShape.StrokeBrush = brush;

        var borderVisual = compositor.CreateShapeVisual();
        borderVisual.Shapes.Add(borderShape);
        panel._hostVisualSizeExpressionAnimation.SetReferenceParameter("hostVisual", elementVisual);
        borderVisual.StartAnimation("Size", panel._hostVisualSizeExpressionAnimation);
        borderVisual.BorderMode = CompositionBorderMode.Soft;

        // 用于定位的本地属性集
        var localProperty = compositor.CreatePropertySet();
        localProperty.InsertVector2("elementPosition", Vector2.Zero);
        panel._ellipseCenterExpressionAnimation.SetReferenceParameter(
            "localProperty",
            localProperty
        );

        // 当子元素布局变化时更新位置
        child.LayoutUpdated += (s, args) =>
        {
            var transform = child.TransformToVisual(panel).TransformPoint(new Point(0, 0));
            localProperty.InsertVector2(
                "elementPosition",
                new Vector2((float)transform.X, (float)transform.Y)
            );
        };

        brush.StartAnimation("EllipseCenter", panel._ellipseCenterExpressionAnimation);

        // 覆盖层（填充形状，带透明度动画）
        panel._hostVisualSizeExpressionAnimation.SetReferenceParameter("hostVisual", elementVisual);
        overlayGeometry.StartAnimation("Size", panel._hostVisualSizeExpressionAnimation);

        var overlayShape = compositor.CreateSpriteShape(overlayGeometry);
        overlayShape.FillBrush = brush;
        var overlayVisual = compositor.CreateShapeVisual();
        overlayVisual.Shapes.Add(overlayShape);
        overlayVisual.StartAnimation("Size", panel._hostVisualSizeExpressionAnimation);
        overlayVisual.Opacity = 0f;

        // 定位到子元素的偏移
        panel._visualOffsetExpressionAnimation.SetReferenceParameter(
            "localProperty",
            localProperty
        );
        overlayVisual.StartAnimation("Offset.XY", panel._visualOffsetExpressionAnimation);
        borderVisual.StartAnimation("Offset.XY", panel._visualOffsetExpressionAnimation);

        // 添加到覆盖容器（Z顺序：边框在上，覆盖在下）
        var overlayVisuals = panel._overlayContainer.Children;
        overlayVisuals.InsertAtTop(overlayVisual);
        overlayVisuals.InsertAtTop(borderVisual);

        // 指针进入/离开 → 控制覆盖层透明度动画
        child.PointerEntered += (sender, args) =>
        {
            if (((FrameworkElement)sender).Visibility == Visibility.Visible)
            {
                overlayVisual.StartAnimation("Opacity", panel._opacityForwardAnimation);
            }
        };
        child.PointerExited += (sender, args) =>
        {
            if (((FrameworkElement)sender).Visibility == Visibility.Visible)
            {
                overlayVisual.StartAnimation("Opacity", panel._opacityBackwardAnimation);
            }
        };

        // 指针按下/释放 → 控制光晕半径动画
        child.AddHandler(
            PointerPressedEvent,
            new PointerEventHandler(
                (s, args) =>
                {
                    brush.StartAnimation("EllipseRadius", panel._revealBrushRadiusForwardAnimation);
                }
            ),
            true
        );

        child.AddHandler(
            PointerReleasedEvent,
            new PointerEventHandler(
                (s, args) =>
                {
                    brush.StartAnimation(
                        "EllipseRadius",
                        panel._revealBrushRadiusBackwardAnimation
                    );
                }
            ),
            true
        );
    }

    // ---------- 辅助方法：绑定圆角 ----------
    private static CornerRadius BindToCornerRadiusProperty(
        FrameworkElement element,
        float halfStroke,
        CompositionRoundedRectangleGeometry borderGeometry,
        CompositionRoundedRectangleGeometry overlayGeometry
    )
    {
        var cornerRadius = new CornerRadius(-1);

        if (element is Control control)
        {
            cornerRadius = control.CornerRadius;
            BindToCornerRadiusPropertyImpl(
                control,
                Control.CornerRadiusProperty,
                halfStroke,
                borderGeometry,
                overlayGeometry
            );
        }
        else if (element is ContentPresenter presenter)
        {
            cornerRadius = presenter.CornerRadius;
            BindToCornerRadiusPropertyImpl(
                presenter,
                ContentPresenter.CornerRadiusProperty,
                halfStroke,
                borderGeometry,
                overlayGeometry
            );
        }
        else if (element is Grid grid)
        {
            cornerRadius = grid.CornerRadius;
            BindToCornerRadiusPropertyImpl(
                grid,
                Grid.CornerRadiusProperty,
                halfStroke,
                borderGeometry,
                overlayGeometry
            );
        }

        return cornerRadius;
    }

    private static void BindToCornerRadiusPropertyImpl(
        DependencyObject obj,
        DependencyProperty property,
        float halfStroke,
        CompositionRoundedRectangleGeometry borderGeometry,
        CompositionRoundedRectangleGeometry overlayGeometry
    )
    {
        obj.RegisterPropertyChangedCallback(
            property,
            (d, dp) =>
            {
                var cornerRadius = (CornerRadius)d.GetValue(dp);
                var adjustedRadius = Math.Max(0, (float)cornerRadius.TopLeft - halfStroke);
                borderGeometry.CornerRadius = new Vector2(adjustedRadius, adjustedRadius);
                overlayGeometry.CornerRadius = new Vector2(
                    (float)cornerRadius.TopLeft,
                    (float)cornerRadius.TopLeft
                );
            }
        );
    }
}

public static class RevealBrush
{
    /// <summary>
    /// 默认的椭圆半径（宽度和高度均为 75）。
    /// </summary>
    public static readonly Vector2 NormalRevealRadius = new(75f, 75f);

    /// <summary>
    /// 创建一个已配置好的径向渐变画刷，用于 Reveal 光晕效果。
    /// </summary>
    /// <param name="compositor">用于创建画刷的 Compositor 实例。</param>
    /// <returns>配置好的 CompositionRadialGradientBrush。</returns>
    public static CompositionRadialGradientBrush Create(Compositor compositor)
    {
        var brush = compositor.CreateRadialGradientBrush();

        // 设置颜色停止点（灰度渐变，从中心半透明白到边缘全透明）
        brush.ColorStops.Add(
            compositor.CreateColorGradientStop(0f, Color.FromArgb(255, 0xb9, 0xb9, 0xb9))
        );
        brush.ColorStops.Add(
            compositor.CreateColorGradientStop(1f, Color.FromArgb(0, 0xb9, 0xb9, 0xb9))
        );

        brush.EllipseRadius = NormalRevealRadius;
        brush.MappingMode = CompositionMappingMode.Absolute;

        return brush;
    }
}
