using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using ZLinq;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayQueuePage : Page
{
    public PlayQueueViewModel ViewModel { get; } = App.GetService<PlayQueueViewModel>();

    public PlayQueuePage()
    {
        InitializeComponent();
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IndexedPlayQueueSong info } menuItem)
        {
            while (menuItem.Items.Count > 3) // 保留前三个固定项目，清除其他动态添加的项目
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = new Tuple<IBriefSongInfoBase, PlaylistInfo>(info.Song, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<IBriefSongInfoBase, PlaylistInfo> tuple })
        {
            var (songInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButton_Click(songInfo, playlist);
        }
    }

    private void AddToFlyout_Opened(object sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            while (flyout.Items.Count > 3)
            {
                flyout.Items.RemoveAt(3);
            }
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = playlist,
                };
                playlistMenuItem.Click += AddToPlaylistFlyoutButton_Click;
                flyout.Items.Add(playlistMenuItem);
            }
        }
    }

    private void AddToPlaylistFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: PlaylistInfo playlist })
        {
            ViewModel.AddToPlaylistFlyoutButton_Click(playlist);
        }
    }

    private void AddToPlayQueueFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddToPlayQueueFlyoutButton_Click();
    }

    private async void AddToNewPlaylistFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            ViewModel.AddToPlaylistFlyoutButton_Click(dialog.CreatedPlaylist);
        }
    }

    private void PlayQueuePage_Loaded(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        if (currentSong is null)
        {
            return;
        }
        var listViewSource = PlayqueueListView.ItemsSource;
        if (listViewSource is IEnumerable<IndexedPlayQueueSong> songs)
        {
            var targetSong = currentSong switch
            {
                BriefLocalSongInfo localSong => songs
                    .AsValueEnumerable()
                    .FirstOrDefault(song => song.Song.Path == localSong.Path),
                IBriefOnlineSongInfo onlineSong => songs
                    .AsValueEnumerable()
                    .FirstOrDefault(song =>
                        song.Song is IBriefOnlineSongInfo s && s.ID == onlineSong.ID
                    ),
                _ => null,
            };
            if (targetSong is not null)
            {
                PlayqueueListView.ScrollIntoView(targetSong, ScrollIntoViewAlignment.Leading);
            }
        }
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        (grid?.FindName("ItemCheckBox") as CheckBox)?.Visibility = Visibility.Visible;
        (grid?.FindName("PlayButton") as Button)?.Visibility = Visibility.Visible;
        (grid?.FindName("MusicFontIcon") as FontIcon)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("PlayingFontIcon") as FontIcon)?.Visibility = Visibility.Collapsed;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        (grid?.FindName("ItemCheckBox") as CheckBox)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("PlayButton") as Button)?.Visibility = Visibility.Collapsed;
        (grid?.FindName("MusicFontIcon") as FontIcon)?.Visibility = Visibility.Visible;
        if (
            grid?.FindName("PlayingFontIcon") is FontIcon playingFontIcon
            && grid.DataContext is IndexedPlayQueueSong songInfo
        )
        {
            var isCurrentlyPlaying = Data.MusicPlayer.PlayQueueIndex == songInfo.Index;
            playingFontIcon.Visibility = isCurrentlyPlaying
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.PlayNextButton_Click(info.Song);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.AddToPlayQueueButton_Click(info.Song);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButton_Click(info.Song, dialog.CreatedPlaylist);
            }
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.RemoveButton_Click(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.MoveUpButton_Click(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.MoveDownButton_Click(info);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info.Song);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.ShowAlbumButton_Click(info.Song);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.ShowArtistButton_Click(info.Song);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
