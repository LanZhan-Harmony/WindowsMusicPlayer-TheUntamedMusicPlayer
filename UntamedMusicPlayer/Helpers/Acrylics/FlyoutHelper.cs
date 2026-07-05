using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class FlyoutHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(FlyoutHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(Flyout flyout) =>
        (bool)flyout.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(Flyout flyout, bool value) =>
        flyout.SetValue(AcrylicWorkaroundProperty, value);

    private static void OnAcrylicWorkaroundChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var flyout = (Flyout)d;

        // 从应用资源获取默认的 FlyoutPresenterStyle
        var defaultStyle = Application.Current.Resources.TryGetValue(
            "DefaultFlyoutPresenterStyle",
            out var obj
        )
            ? obj as Style
            : null;
        if (defaultStyle is null)
        {
            return;
        }

        // 获取现有的 FlyoutPresenterStyle（可能为 null）
        var previousStyle = flyout.FlyoutPresenterStyle;

        // 创建新样式，基于现有样式或默认样式
        var transparentStyle = new Style(typeof(FlyoutPresenter))
        {
            BasedOn = previousStyle ?? defaultStyle,
        };

        // 将背景设为透明
        transparentStyle.Setters.Add(
            new Setter(Control.BackgroundProperty, new SolidColorBrush(Colors.Transparent))
        );

        // 应用新样式
        flyout.FlyoutPresenterStyle = transparentStyle;

        // 允许 Flyout 超出根边界
        flyout.ShouldConstrainToRootBounds = false;

        // 设置系统背景为亚克力
        flyout.SystemBackdrop = new DesktopAcrylicBackdrop();
    }
}
