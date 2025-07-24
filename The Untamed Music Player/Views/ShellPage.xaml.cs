using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    private readonly string _appTitleBarText = "AppDisplayName".GetLocalized();

    public ShellPage() //注意修改, 不能有参数
    {
        InitializeComponent();
        Data.ShellPage = this;
        ViewModel = App.GetService<ShellViewModel>();
        Data.MainWindow!.SetTitleBar(AppTitleBar);
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

    public NavigationView GetNavigationView()
    {
        return NavigationViewControl;
    }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsFirstLoaded)
        {
            var pageToNavigate = ViewModel.CurrentPage switch
            {
                nameof(HomePage) => nameof(HomePage),
                nameof(MusicLibraryPage) => nameof(MusicLibraryPage),
                nameof(PlayQueuePage) => nameof(PlayQueuePage),
                nameof(PlayListsPage) => nameof(PlayListsPage),
                nameof(SettingsPage) => nameof(SettingsPage),
                _ => nameof(HomePage),
            };
            Navigate(pageToNavigate, "", new SuppressNavigationTransitionInfo());
            ViewModel.IsFirstLoaded = false;
        }
        /*else
        {
            Navigate(
                ViewModel.CurrentPage,
                ViewModel.NavigatePage,
                new SuppressNavigationTransitionInfo()
            );
        }*/
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
            Navigate($"{invokedItem.Tag}");
        }
    }

    public void Navigate(string tag)
    {
        switch (tag)
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

    public void Navigate(
        string destPage,
        string parameter,
        NavigationTransitionInfo? infoOverride = null
    )
    {
        ViewModel.NavigatePage = parameter;
        var pageToNavigate = destPage switch
        {
            nameof(HomePage) => typeof(HomePage),
            nameof(MusicLibraryPage) => typeof(MusicLibraryPage),
            nameof(PlayQueuePage) => typeof(PlayQueuePage),
            nameof(PlayListsPage) => typeof(PlayListsPage),
            nameof(SettingsPage) => typeof(SettingsPage),
            nameof(LocalAlbumDetailPage) => typeof(LocalAlbumDetailPage),
            nameof(LocalArtistDetailPage) => typeof(LocalArtistDetailPage),
            nameof(OnlineAlbumDetailPage) => typeof(OnlineAlbumDetailPage),
            nameof(OnlineArtistDetailPage) => typeof(OnlineArtistDetailPage),
            _ => typeof(HomePage),
        };
        NavigationFrame.Navigate(pageToNavigate, null, infoOverride);
    }
}
