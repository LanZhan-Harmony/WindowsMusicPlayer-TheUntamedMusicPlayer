using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using ZLinq;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class NavigationViewHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(NavigationViewHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(NavigationView navigationView) =>
        (bool)navigationView.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(NavigationView navigationView, bool value) =>
        navigationView.SetValue(AcrylicWorkaroundProperty, value);

    public static readonly DependencyProperty ClipToBoundsProperty =
        DependencyProperty.RegisterAttached(
            "ClipToBounds",
            typeof(bool),
            typeof(NavigationViewHelper),
            new PropertyMetadata(false, OnClipToBoundsChanged)
        );

    public static bool GetClipToBounds(NavigationView element) =>
        (bool)element.GetValue(ClipToBoundsProperty);

    public static void SetClipToBounds(NavigationView element, bool value) =>
        element.SetValue(ClipToBoundsProperty, value);

    // AcrylicWorkaround 属性变更回调
    private static void OnAcrylicWorkaroundChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var navigationView = (NavigationView)d;
        var weakNav = new WeakReference<NavigationView>(navigationView);

        void loadedHandler(object sender, RoutedEventArgs args)
        {
            if (!weakNav.TryGetTarget(out var nav))
            {
                (sender as NavigationView)?.Loaded -= loadedHandler;
                return;
            }

            // 查找 TopNavOverflowButton
            var button = nav.FindDescendants()
                .AsValueEnumerable()
                .OfType<Button>()
                .FirstOrDefault(btn => btn.Name == "TopNavOverflowButton");
            if (button is null)
            {
                return;
            }

            if (button.Flyout is not Flyout flyout)
            {
                return;
            }

            // 获取新样式
            var newStyle = GetStyle(flyout.FlyoutPresenterStyle);

            // 应用样式
            flyout.FlyoutPresenterStyle = newStyle;
            flyout.ShouldConstrainToRootBounds = false;
            flyout.SystemBackdrop = new DesktopAcrylicBackdrop();

            // 修改所有菜单项的 Flyout
            ModifyNavigationViewItems(nav, newStyle);

            // 取消 Loaded 订阅
            nav.Loaded -= loadedHandler;
        }

        navigationView.Loaded += loadedHandler;
    }

    // ClipToBounds 属性变更回调
    private static void OnClipToBoundsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var navigationView = (NavigationView)d;
        var clipToBounds = (bool)e.NewValue;

        var visual = ElementCompositionPreview.GetElementVisual(navigationView);
        if (!clipToBounds)
        {
            visual.Clip = null;
            return;
        }

        var compositor = visual.Compositor;
        var clipGeometry = compositor.CreateRoundedRectangleGeometry();
        var sizeExpressionAnimation = compositor.CreateExpressionAnimation("host.Size");
        sizeExpressionAnimation.SetReferenceParameter("host", visual);
        var cornerRadius = (float)navigationView.CornerRadius.TopLeft;
        clipGeometry.CornerRadius = new System.Numerics.Vector2(cornerRadius, cornerRadius);
        clipGeometry.StartAnimation("Size", sizeExpressionAnimation);
        visual.Clip = compositor.CreateGeometricClip(clipGeometry);

        // 监听 CornerRadius 变化
        navigationView.RegisterPropertyChangedCallback(
            Control.CornerRadiusProperty,
            (sender, dp) =>
            {
                var radius = (CornerRadius)navigationView.GetValue(dp);
                var newRadius = (float)radius.TopLeft;
                clipGeometry.CornerRadius = new System.Numerics.Vector2(newRadius, newRadius);
            }
        );
    }

    // 辅助方法：修改 NavigationView 的菜单项 Flyout
    private static void ModifyNavigationViewItems(NavigationView navigationView, Style newStyle)
    {
        foreach (var item in navigationView.MenuItems)
        {
            if (item is not NavigationViewItem navItem)
            {
                continue;
            }

            // 获取第一个子元素（通常是 Grid）
            if (VisualTreeHelper.GetChild(navItem, 0) is not FrameworkElement rootGrid)
            {
                continue;
            }

            var attachedFlyout = FlyoutBase.GetAttachedFlyout(rootGrid);
            if (attachedFlyout is not Flyout flyout)
            {
                continue;
            }

            flyout.FlyoutPresenterStyle = newStyle;
            flyout.ShouldConstrainToRootBounds = false;
            flyout.SystemBackdrop = new DesktopAcrylicBackdrop();

            // 订阅 Opening 事件
            flyout.Opening += OnFlyoutOpening;
        }
    }

    // Flyout.Opening 事件处理
    private static void OnFlyoutOpening(object? sender, object e)
    {
        var flyout = (Flyout)sender!;
        var content = flyout.Content;
        if (content is ItemsRepeaterScrollHost host)
        {
            host.ScrollViewer.Background = null;
        }
    }

    // 获取新样式（基于 DefaultFlyoutPresenterStyle）
    private static Style GetStyle(Style defaultNavigationViewFlyoutStyle)
    {
        var newStyle = new Style(typeof(FlyoutPresenter));

        // 基于 DefaultFlyoutPresenterStyle
        if (
            Application.Current.Resources.TryGetValue(
                "DefaultFlyoutPresenterStyle",
                out var defaultStyleObj
            ) && defaultStyleObj is Style defaultStyle
        )
        {
            newStyle.BasedOn = defaultStyle;
        }

        // 复制原样式中的 CornerRadius、Padding、Margin
        foreach (var setter in defaultNavigationViewFlyoutStyle.Setters)
        {
            if (setter is Setter s)
            {
                if (
                    s.Property == Control.CornerRadiusProperty
                    || s.Property == Control.PaddingProperty
                    || s.Property == FrameworkElement.MarginProperty
                )
                {
                    newStyle.Setters.Add(s);
                }
            }
        }

        // 添加透明背景
        newStyle.Setters.Add(
            new Setter(Control.BackgroundProperty, new SolidColorBrush(Colors.Transparent))
        );

        return newStyle;
    }
}
