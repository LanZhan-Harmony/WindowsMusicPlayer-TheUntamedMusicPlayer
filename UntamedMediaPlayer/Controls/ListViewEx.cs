using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// Compatibility ListView used by the reverse engineered XAML.
/// The original UWP app exposed these values on a custom ListViewBase type;
/// keeping them as dependency properties lets the migrated markup compile
/// while retaining the values for templates and later interaction logic.
/// </summary>
public sealed partial class ListViewEx : ListView
{
    public bool IsItemDoubleClickEnabled
    {
        get => (bool)GetValue(IsItemDoubleClickEnabledProperty);
        set => SetValue(IsItemDoubleClickEnabledProperty, value);
    }

    public static readonly DependencyProperty IsItemDoubleClickEnabledProperty =
        DependencyProperty.Register(
            nameof(IsItemDoubleClickEnabled),
            typeof(bool),
            typeof(ListViewEx),
            new PropertyMetadata(false)
        );

    public string? UseCustomItemPresenterWithDisplayMode
    {
        get => (string?)GetValue(UseCustomItemPresenterWithDisplayModeProperty);
        set => SetValue(UseCustomItemPresenterWithDisplayModeProperty, value);
    }

    public static readonly DependencyProperty UseCustomItemPresenterWithDisplayModeProperty =
        DependencyProperty.Register(
            nameof(UseCustomItemPresenterWithDisplayMode),
            typeof(string),
            typeof(ListViewEx),
            new PropertyMetadata(null)
        );

    public MenuFlyout? ItemsMenuFlyout
    {
        get => (MenuFlyout?)GetValue(ItemsMenuFlyoutProperty);
        set => SetValue(ItemsMenuFlyoutProperty, value);
    }

    public static readonly DependencyProperty ItemsMenuFlyoutProperty = DependencyProperty.Register(
        nameof(ItemsMenuFlyout),
        typeof(MenuFlyout),
        typeof(ListViewEx),
        new PropertyMetadata(null, OnItemsMenuFlyoutChanged)
    );

    private static void OnItemsMenuFlyoutChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is ListViewEx listView)
        {
            // A single flyout can be used as the list's context flyout. Item
            // specific cloning can be added later without changing the XAML API.
            listView.ContextFlyout = args.NewValue as MenuFlyout;
        }
    }
}
