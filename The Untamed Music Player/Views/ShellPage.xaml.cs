using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class ShellPage : Page, IRecipient<HavePlaylistMessage>
{
    public ShellViewModel ViewModel { get; }

    private readonly string _appTitleBarText = "AppDisplayName".GetLocalized();

    public ShellPage() //注意修改, 不能有参数
    {
        StrongReferenceMessenger.Default.Register(this);
        InitializeComponent();
        Data.ShellPage = this;
        ViewModel = App.GetService<ShellViewModel>();
        Data.MainWindow!.SetTitleBar(AppTitleBar);
    }

    public void Receive(HavePlaylistMessage message)
    {
        PlaylistsNavItem.MenuItems.Clear();
        if (!message.HasPlaylist)
        {
            return;
        }
        foreach (var playlist in Data.PlaylistLibrary.Playlists)
        {
            var playlistItem = new NavigationViewItem
            {
                Content = playlist.Name,
                Tag = nameof(PlayListDetailPage),
                DataContext = playlist,
            };
            ToolTipService.SetToolTip(playlistItem, playlist.Name);
            PlaylistsNavItem.MenuItems.Add(playlistItem);
        }
    }

    private void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
    }

    private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.Unregister<HavePlaylistMessage>(this);
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
                nameof(HomePage)
                or nameof(OnlineAlbumDetailPage)
                or nameof(OnlineArtistDetailPage)
                or nameof(OnlinePlayListDetailPage) => nameof(HomePage),

                nameof(MusicLibraryPage)
                or nameof(LocalAlbumDetailPage)
                or nameof(LocalArtistDetailPage) => nameof(MusicLibraryPage),

                nameof(PlayQueuePage) => nameof(PlayQueuePage),
                nameof(PlayListsPage) or nameof(PlayListDetailPage) => nameof(PlayListsPage),
                nameof(SettingsPage) => nameof(SettingsPage),
                _ => nameof(HomePage),
            };
            Navigate(pageToNavigate, "", new SuppressNavigationTransitionInfo());
            ViewModel.IsFirstLoaded = false;
        }
    }

    private void PlaylistsNavItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationViewItem navItem)
        {
            navItem.MenuItems.Clear();
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistItem = new NavigationViewItem
                {
                    Content = playlist.Name,
                    Tag = nameof(PlayListDetailPage),
                    DataContext = playlist,
                };
                ToolTipService.SetToolTip(playlistItem, playlist.Name);
                navItem.MenuItems.Add(playlistItem);
            }
        }
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
            var tag = $"{invokedItem.Tag}";
            if (
                tag == nameof(PlayListDetailPage)
                && invokedItem.DataContext is PlaylistInfo playlist
            )
            {
                if (ViewModel.PrevPlaylistInfo == playlist)
                {
                    return;
                }
                Data.SelectedPlaylist = playlist;
                ViewModel.PrevPlaylistInfo = playlist;
            }
            else if (ViewModel.CurrentPage == tag)
            {
                return; // 避免重复导航到同一页面
            }
            else
            {
                ViewModel.PrevPlaylistInfo = null;
            }
            var pageToNavigate = tag switch
            {
                nameof(HomePage) => typeof(HomePage),
                nameof(MusicLibraryPage) => typeof(MusicLibraryPage),
                nameof(PlayQueuePage) => typeof(PlayQueuePage),
                nameof(PlayListsPage) => typeof(PlayListsPage),
                nameof(PlayListDetailPage) => typeof(PlayListDetailPage),
                nameof(SettingsPage) => typeof(SettingsPage),
                _ => typeof(HomePage),
            };
            NavigationFrame.Navigate(pageToNavigate);
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
            nameof(PlayListDetailPage) => typeof(PlayListDetailPage),
            nameof(OnlineAlbumDetailPage) => typeof(OnlineAlbumDetailPage),
            nameof(OnlineArtistDetailPage) => typeof(OnlineArtistDetailPage),
            nameof(OnlinePlayListDetailPage) => typeof(OnlinePlayListDetailPage),
            _ => typeof(HomePage),
        };
        NavigationFrame.Navigate(pageToNavigate, null, infoOverride);
    }

    public void GoBack()
    {
        if (NavigationFrame.CanGoBack)
        {
            var page = NavigationFrame.BackStack.LastOrDefault();
            if (page?.SourcePageType == typeof(PlayListDetailPage) && Data.SelectedPlaylist is null)
            {
                NavigationFrame.BackStack.Remove(page);
                GoBack();
            }
            else
            {
                NavigationFrame.GoBack();
            }
        }
    }
}
