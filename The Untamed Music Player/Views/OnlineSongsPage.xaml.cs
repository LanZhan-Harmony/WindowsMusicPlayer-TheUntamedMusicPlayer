using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using ZLinq;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineSongsPage : Page
{
    public OnlineSongsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlineSongsPage()
    {
        ViewModel = App.GetService<OnlineSongsViewModel>();
        InitializeComponent();
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
            Data.MusicPlayer.CurrentBriefSong is IBriefOnlineSongInfo currentSong
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
}
