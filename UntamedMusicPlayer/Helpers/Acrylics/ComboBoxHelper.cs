using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ZLinq;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class ComboBoxHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(ComboBoxHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(ComboBox box) =>
        (bool)box.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(ComboBox box, bool value) =>
        box.SetValue(AcrylicWorkaroundProperty, value);

    private static void OnAcrylicWorkaroundChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var comboBox = (ComboBox)d;
        var weakBox = new WeakReference<ComboBox>(comboBox);

        void layoutUpdatedHandler(object? sender, object args)
        {
            if (!weakBox.TryGetTarget(out var box))
            {
                (sender as ComboBox)?.LayoutUpdated -= layoutUpdatedHandler;
                return;
            }

            // 查找名为 "Popup" 的 Popup
            var popup = box.FindDescendants()
                .AsValueEnumerable()
                .OfType<Popup>()
                .FirstOrDefault(p => p.Name == "Popup");
            if (popup is null)
            {
                return;
            }

            // 查找名为 "PopupBorder" 的 Border
            var borderObj = popup.FindName("PopupBorder");
            if (borderObj is not Border border)
            {
                return;
            }

            box.LayoutUpdated -= layoutUpdatedHandler;

            void loadedHandler(object sender, RoutedEventArgs args)
            {
                border.Loaded -= loadedHandler;

                // 保存原始子元素
                var originalChild = border.Child;

                // 创建新 Grid 作为容器
                var grid = new Grid();
                border.Child = grid;

                // 添加亚克力背景（带圆角绑定）
                grid.Children.Add(new AcrylicVisualWithBoundedCornerRadius(border));
                if (originalChild is not null)
                {
                    grid.Children.Add(originalChild);
                }

                // ---- 额外修复：非可编辑且无滚动条时的下拉重开 ----
                if (!box.IsEditable)
                {
                    var scrollViewer = border
                        .FindDescendants()
                        .AsValueEnumerable()
                        .OfType<ScrollViewer>()
                        .FirstOrDefault();

                    if (
                        scrollViewer is not null
                        && scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Collapsed
                    )
                    {
                        void dropDownClosedHandler(object? s, object ev)
                        {
                            box.DropDownClosed -= dropDownClosedHandler;
                            box.IsDropDownOpen = true;
                        }
                        box.DropDownClosed += dropDownClosedHandler;
                    }
                }
            }

            border.Loaded += loadedHandler;
        }

        comboBox.LayoutUpdated += layoutUpdatedHandler;
    }
}
