using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Messages;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using ZLinq;

namespace UntamedMusicPlayer.Views;

public sealed partial class ShellPage : Page, IRecipient<HavePlaylistMessage>
{
    public ShellViewModel ViewModel { get; }

    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly string _appTitleBarText = "AppDisplayName".GetLocalized();

    public ShellPage() //注意修改, 不能有参数
    {
        InitializeComponent();
        ViewModel = App.GetService<ShellViewModel>();
        _navigationService.InitializeShell(
            this,
            NavigationFrame,
            NavigationViewControl,
            page => ViewModel.NavigatePage = page
        );
        App.GetService<IWindowService>().SetTitleBar(AppTitleBar);
    }

    public void Receive(HavePlaylistMessage message)
    {
        PlaylistsNavItem.MenuItems.Clear();
        if (!message.HasPlaylist)
        {
            return;
        }
        foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
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
        StrongReferenceMessenger.Default.Register(this);
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
            Navigate(pageToNavigate, "", NavigationTransition.Suppress);
            ViewModel.IsFirstLoaded = false;
        }
    }

    private void PlaylistsNavItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationViewItem navItem)
        {
            navItem.MenuItems.Clear();
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
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
            _navigationService.GoBackShell();
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
                ViewModel.PrevPlaylistInfo = playlist;
                _navigationService.NavigateShell(
                    nameof(PlayListDetailPage),
                    new PlaylistNavigationArgs(playlist, nameof(ShellPage))
                );
                return;
            }
            else if (ViewModel.CurrentPage == tag)
            {
                return; // 避免重复导航到同一页面
            }
            else
            {
                ViewModel.PrevPlaylistInfo = null;
            }
            Navigate(tag, null);
        }
    }

    private void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        var pageName = e.SourcePageType.Name;
        _ = ViewModel.SetCurrentPageAsync(pageName, e.NavigationMode == NavigationMode.Back);
        SetSelectedNavigationItem(pageName);
    }

    private void SetSelectedNavigationItem(string pageName)
    {
        if (
            pageName
            is nameof(HomePage)
                or nameof(OnlineAlbumDetailPage)
                or nameof(OnlineArtistDetailPage)
                or nameof(OnlinePlayListDetailPage)
        )
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        }
        else if (
            pageName
            is nameof(MusicLibraryPage)
                or nameof(LocalAlbumDetailPage)
                or nameof(LocalArtistDetailPage)
        )
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
        }
        else if (pageName is nameof(PlayQueuePage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[3];
        }
        else if (pageName is nameof(PlayListsPage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[4];
        }
        else if (pageName is nameof(PlayListDetailPage))
        {
            var playlistsNavItem = NavigationViewControl.MenuItems[4] as NavigationViewItem;
            var playlistSubItem = playlistsNavItem!
                .MenuItems.AsValueEnumerable()
                .Cast<NavigationViewItem>()
                .FirstOrDefault(item =>
                    item.DataContext is PlaylistInfo playlist
                    && playlist == ViewModel.PrevPlaylistInfo
                );
            NavigationViewControl.SelectedItem = playlistSubItem ?? playlistsNavItem;
        }
        else if (pageName is nameof(SettingsPage))
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.FooterMenuItems[0];
        }
    }

    private void NavigationFrame_DragOver(object sender, DragEventArgs e)
    {
        if (
            !ViewModel.CanAcceptExternalStorageItems()
            || !e.DataView.Contains(StandardDataFormats.StorageItems)
        )
        {
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Shell_PlayFiles".GetLocalized();
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
        e.DragUIOverride.IsGlyphVisible = false;
    }

    private async void NavigationFrame_Drop(object sender, DragEventArgs e)
    {
        if (
            !ViewModel.CanAcceptExternalStorageItems()
            || !e.DataView.Contains(StandardDataFormats.StorageItems)
        )
        {
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            await ViewModel.AddExternalStorageItemsToPlayQueueAsync(items);
        }
        finally
        {
            deferral.Complete();
        }
    }

    public void Navigate(
        string destPage,
        object? parameter,
        NavigationTransition transition = NavigationTransition.Default
    )
    {
        _navigationService.NavigateShell(destPage, parameter, transition);
    }

    public void GoBack()
    {
        _navigationService.GoBackShell();
    }
}
