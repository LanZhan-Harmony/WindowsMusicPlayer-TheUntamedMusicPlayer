using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Views;

public sealed partial class ShellPage : Page
{
    private readonly string _appTitleBarText = "AppDisplayName".GetLocalized();

    public ShellPage() //注意修改, 不能有参数
    {
        InitializeComponent();

        Data.MainWindow!.SetTitleBar(AppTitleBar);
        Data.ShellPage = this;
    }

    public void NavigationViewControl_DisplayModeChanged(
        NavigationView sender,
        NavigationViewDisplayModeChangedEventArgs args
    )
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left =
                sender.CompactPaneLength
                * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom,
        };
    }

    public Frame GetFrame()
    {
        return NavigationFrame;
    }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs e)
    {
        Navigate("Home");
    }

    private void NavigationViewControl_BackRequested(
        NavigationView sender,
        NavigationViewBackRequestedEventArgs args
    )
    {
        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
        }
    }

    private void NavigationViewControl_ItemInvoked(
        NavigationView sender,
        NavigationViewItemInvokedEventArgs args
    )
    {
        if (args.InvokedItemContainer is NavigationViewItem invokedItem)
        {
            Navigate(invokedItem.Tag.ToString()!);
        }
    }

    public void Navigate(string Tag)
    {
        switch (Tag)
        {
            case "Home":
                NavigationFrame.Navigate(typeof(HomePage));
                break;
            case "MusicLibrary":
                NavigationFrame.Navigate(typeof(MusicLibraryPage));
                break;
            case "PlayQueue":
                NavigationFrame.Navigate(typeof(PlayQueuePage));
                break;
            case "PlayLists":
                NavigationFrame.Navigate(typeof(PlayListsPage));
                break;
            case "Settings":
                NavigationFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }

    private void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.SourcePageType == typeof(HomePage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        }
        else if (e.SourcePageType == typeof(MusicLibraryPage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
        }
        else if (e.SourcePageType == typeof(PlayQueuePage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[3];
        }
        else if (e.SourcePageType == typeof(PlayListsPage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[4];
        }
        else if (e.SourcePageType == typeof(SettingsPage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.FooterMenuItems[0];
        }
    }
}
