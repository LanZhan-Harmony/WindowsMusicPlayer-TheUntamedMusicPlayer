using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;
using Windows.System;
using Windows.UI.Core;
using ZLinq;

namespace UntamedMusicPlayer.Views;

public sealed partial class OnlineSongsPage : Page
{
    public OnlineSongsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlineSongsPage()
    {
        ViewModel = App.GetService<OnlineSongsViewModel>();
        InitializeComponent();
        Data.OnlineSongsPage = this;
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IBriefOnlineSongInfo info } menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = new Tuple<IBriefOnlineSongInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (
            sender is MenuFlyoutItem
            {
                DataContext: Tuple<IBriefOnlineSongInfo, PlaylistInfo> tuple
            }
        )
        {
            var (songInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButton_Click(songInfo, playlist);
        }
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
    }

    private void OnlineSongsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer =
            SongListView.FindDescendant<ScrollViewer>()
            ?? throw new Exception("Cannot find ScrollViewer in ListView"); // 检索 ListView 内部使用的 ScrollViewer

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (
                !Data.OnlineMusicLibrary.OnlineSongInfoList.HasAllLoaded
                && _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight
                    >= _scrollViewer.ExtentHeight - 50
            )
            {
                await Data.OnlineMusicLibrary.SearchMore();
                await Task.Delay(3000);
            }
        };

        if (
            Data.PlayState.CurrentBriefSong is IBriefOnlineSongInfo currentSong
            && SongListView.ItemsSource is IEnumerable<IBriefOnlineSongInfo> songs
        )
        {
            var targetSong = songs
                .AsValueEnumerable()
                .FirstOrDefault(song => song.ID == currentSong.ID);
            if (targetSong is not null)
            {
                SongListView.ScrollIntoView(targetSong, ScrollIntoViewAlignment.Leading);
            }
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.OnlineSongsPlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.OnlineSongsPlayNextButton_Click(info);
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            await DownloadHelper.DownloadOnlineSongAsync(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.AddToPlayQueueButton_Click(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButton_Click(info, dialog.CreatedPlaylist);
            }
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }

    public string GetAutomationName(string album, string artistsStr, string title)
    {
        return $"{album}, {artistsStr}, {title}";
    }

    /// <summary>
    /// 为 MenuFlyout 的所有项设置 DataContext（递归）
    /// </summary>
    private static void SetFlyoutItemsDataContext(MenuFlyout flyout, object? dataContext)
    {
        foreach (var item in flyout.Items)
        {
            switch (item)
            {
                case MenuFlyoutItem menuItem:
                    menuItem.DataContext = dataContext;
                    break;
                case MenuFlyoutSubItem subItem:
                    subItem.DataContext = dataContext;
                    // 子项也要设置
                    foreach (var child in subItem.Items)
                    {
                        if (child is MenuFlyoutItem childItem)
                        {
                            childItem.DataContext = dataContext;
                        }
                    }
                    break;
                case MenuFlyoutSeparator:
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// 处理键盘快捷键打开上下文菜单
    /// </summary>
    private void ContextMenuKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (SongListView.SelectedItem is IBriefOnlineSongInfo selectedSong)
        {
            var container = SongListView.ContainerFromItem(selectedSong) as ListViewItem;
            if (container is not null)
            {
                var grid = container.ContentTemplateRoot as Grid;
                if (grid?.ContextFlyout is MenuFlyout flyout)
                {
                    // 将 DataContext 设置到每个子项上，MenuFlyout 本身没有 DataContext
                    SetFlyoutItemsDataContext(flyout, selectedSong);

                    // 对于可能依赖 Loaded 事件动态填充的子项，先设置 DataContext 后手动触发加载逻辑
                    foreach (var sub in flyout.Items.OfType<MenuFlyoutSubItem>())
                    {
                        if (sub.Items.Count <= 3)
                        {
                            // 确保 sub.DataContext 已正确设置
                            sub.DataContext = selectedSong;
                            AddToSubItem_Loaded(sub, new RoutedEventArgs());
                        }
                    }

                    flyout.ShowAt(container);
                    args.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// 处理在列表项上按键
    /// </summary>
    private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // 支持在焦点在列表项时按 Menu 键或 Shift+F10 打开上下文菜单
        if (e.Key == VirtualKey.Application || 
            (e.Key == VirtualKey.F10 && 
             (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & 
              CoreVirtualKeyStates.Down) != 0))
        {
            if (sender is Grid grid && grid.ContextFlyout is MenuFlyout flyout)
            {
                // 将 DataContext 设置到每个子项上
                SetFlyoutItemsDataContext(flyout, grid.DataContext);

                foreach (var sub in flyout.Items.OfType<MenuFlyoutSubItem>())
                {
                    if (sub.Items.Count <= 3)
                    {
                        sub.DataContext = grid.DataContext;
                        AddToSubItem_Loaded(sub, new RoutedEventArgs());
                    }
                }

                flyout.ShowAt(grid);
                e.Handled = true;
            }
        }
    }
}
