using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;
using ZLinq;

namespace UntamedMusicPlayer.Views;

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

        // 注册全局键盘事件
        AddHandler(KeyDownEvent, new KeyEventHandler(ShellPage_KeyDown), true);
        
        // 注册全局鼠标事件
        AddHandler(PointerPressedEvent, new PointerEventHandler(ShellPage_PointerPressed), true);
    }

    /// <summary>
    /// 处理全局键盘按键事件
    /// </summary>
    private void ShellPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // 检查是否按下 Esc 键
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            // 检查当前是否有弹出窗口（如对话框、Flyout等）打开
            // 如果有弹出窗口，不执行后退操作，让弹出窗口自己处理 Esc
            if (!IsPopupOpen())
            {
                GoBack();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// 处理全局鼠标按键事件
    /// </summary>
    private void ShellPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        
        // 检查是否按下鼠标侧键（XButton1，通常是后退键）
        if (properties.IsXButton1Pressed)
        {
            GoBack();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 检查是否有弹出窗口打开
    /// </summary>
    private bool IsPopupOpen()
    {
        // 检查是否有打开的 Popup（如 ContentDialog、Flyout、MenuFlyout 等）
        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot);
        if (popups is not null)
        {
            foreach (var popup in popups)
            {
                // 检查 Popup 的 Child 是否为 ContentDialog
                if (popup.Child is ContentDialog)
                {
                    return true;
                }
                
                // 检查是否有其他弹出内容（FlyoutPresenter 用于 Flyout 和 MenuFlyout）
                if (popup.Child is FlyoutPresenter)
                {
                    return true;
                }
            }
        }
        
        return false;
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

    private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.Unregister<HavePlaylistMessage>(this);
        
        // 移除事件处理器
        RemoveHandler(KeyDownEvent, new KeyEventHandler(ShellPage_KeyDown));
        RemoveHandler(PointerPressedEvent, new PointerEventHandler(ShellPage_PointerPressed));
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
            var page = NavigationFrame.BackStack.AsValueEnumerable().LastOrDefault();
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
