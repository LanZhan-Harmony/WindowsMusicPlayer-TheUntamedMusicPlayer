using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class ToolTipHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(ToolTipHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(ToolTip toolTip) =>
        (bool)toolTip.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(ToolTip toolTip, bool value) =>
        toolTip.SetValue(AcrylicWorkaroundProperty, value);

    private static void OnAcrylicWorkaroundChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var toolTip = (ToolTip)d;

        // 使用 Loading 事件，确保在弹出前完成设置
        void loadingHandler(object sender, object args)
        {
            toolTip.Loading -= loadingHandler;

            // 创建自定义亚克力背景
            var backdrop = new CustomAcrylicBackdrop();

            // 获取父级 Popup
            if (toolTip.Parent is Popup popup)
            {
                // 仅当 Popup 尚未设置 SystemBackdrop 时才设置
                popup.SystemBackdrop ??= backdrop;
            }

            // 将工具提示背景设为透明，使亚克力可见
            toolTip.Background = new SolidColorBrush(Colors.Transparent);

            // 同步主题
            backdrop.RequestedTheme = toolTip.ActualTheme;
        }

        toolTip.Loading += loadingHandler;
    }
}
