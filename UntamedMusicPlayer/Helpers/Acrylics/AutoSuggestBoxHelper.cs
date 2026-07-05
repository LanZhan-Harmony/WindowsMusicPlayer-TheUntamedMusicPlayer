using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using ZLinq;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public static class AutoSuggestBoxHelper
{
    public static readonly DependencyProperty AcrylicWorkaroundProperty =
        DependencyProperty.RegisterAttached(
            "AcrylicWorkaround",
            typeof(bool),
            typeof(AutoSuggestBoxHelper),
            new PropertyMetadata(false, OnAcrylicWorkaroundChanged)
        );

    public static bool GetAcrylicWorkaround(AutoSuggestBox box) =>
        (bool)box.GetValue(AcrylicWorkaroundProperty);

    public static void SetAcrylicWorkaround(AutoSuggestBox box, bool value) =>
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

        var autoSuggestBox = (AutoSuggestBox)d;

        var weakBox = new WeakReference<AutoSuggestBox>(autoSuggestBox);

        void layoutUpdatedHandler(object? s, object args)
        {
            if (!weakBox.TryGetTarget(out var box))
            {
                (s as AutoSuggestBox)?.LayoutUpdated -= layoutUpdatedHandler;
                return;
            }

            var popup = box.FindDescendants()
                .AsValueEnumerable()
                .OfType<Popup>()
                .FirstOrDefault(p => p.Name == "SuggestionsPopup");
            if (popup is null)
            {
                return;
            }

            var borderObj = popup.FindName("SuggestionsContainer");
            if (borderObj is not Border border)
            {
                return;
            }

            box.LayoutUpdated -= layoutUpdatedHandler;

            border.Padding = new Thickness(0);
            border.Background = new SolidColorBrush(Colors.Transparent);

            void loadedHandler(object sender, RoutedEventArgs e)
            {
                border.Loaded -= loadedHandler;

                var originalChild = border.Child;
                var grid = new Grid();
                border.Child = grid;

                grid.Children.Add(new AcrylicVisualWithBoundedCornerRadius(border));
                grid.Children.Add(originalChild);
            }

            border.Loaded += loadedHandler;
        }

        autoSuggestBox.LayoutUpdated += layoutUpdatedHandler;
    }
}
