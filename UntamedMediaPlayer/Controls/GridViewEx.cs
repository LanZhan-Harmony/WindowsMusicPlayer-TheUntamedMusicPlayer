using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// GridView compatibility surface used by the recovered XAML.
/// </summary>
internal sealed partial class GridViewEx : GridView
{
    public string? UseCustomItemPresenterWithDisplayMode
    {
        get => (string?)GetValue(UseCustomItemPresenterWithDisplayModeProperty);
        set => SetValue(UseCustomItemPresenterWithDisplayModeProperty, value);
    }

    private static readonly DependencyProperty UseCustomItemPresenterWithDisplayModeProperty =
        DependencyProperty.Register(
            nameof(UseCustomItemPresenterWithDisplayMode),
            typeof(string),
            typeof(GridViewEx),
            new PropertyMetadata(null)
        );

    public MenuFlyout? ItemsMenuFlyout
    {
        get => (MenuFlyout?)GetValue(ItemsMenuFlyoutProperty);
        set => SetValue(ItemsMenuFlyoutProperty, value);
    }

    private static readonly DependencyProperty ItemsMenuFlyoutProperty =
        DependencyProperty.Register(
            nameof(ItemsMenuFlyout),
            typeof(MenuFlyout),
            typeof(GridViewEx),
            new PropertyMetadata(null, OnItemsMenuFlyoutChanged)
        );

    private static void OnItemsMenuFlyoutChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is GridViewEx gridView)
        {
            gridView.ContextFlyout = args.NewValue as MenuFlyout;
        }
    }
}
