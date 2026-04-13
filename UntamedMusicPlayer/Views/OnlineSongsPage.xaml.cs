using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;
using ZLinq;

namespace UntamedMusicPlayer.Views;

public sealed partial class OnlineSongsPage : Page
{
    public OnlineSongsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;
    private bool _isSearching;

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

    private void SongListView_Loaded(object sender, RoutedEventArgs e)
    {
        var listView = (sender as ListView)!;
        if (listView.Visibility == Visibility.Collapsed)
        {
            return;
        }

        _scrollViewer = listView.FindDescendant<ScrollViewer>()!;
        _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

        if (
            Data.PlayState.CurrentBriefSong is IBriefOnlineSongInfo currentSong
            && listView.ItemsSource is IEnumerable<IBriefOnlineSongInfo> songs
        )
        {
            var targetSong = songs
                .AsValueEnumerable()
                .FirstOrDefault(song => song.ID == currentSong.ID);
            if (targetSong is not null)
            {
                listView.ScrollIntoView(targetSong, ScrollIntoViewAlignment.Leading);
            }
        }
    }

    private async void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (
            !_isSearching
            && !Data.OnlineMusicLibrary.OnlineSongInfoList.HasAllLoaded
            && _scrollViewer!.VerticalOffset + _scrollViewer.ViewportHeight
                >= _scrollViewer.ExtentHeight - 50
        )
        {
            _isSearching = true;
            await Data.OnlineMusicLibrary.SearchMore();
            _isSearching = false;
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

    private void OnlineSongsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer?.ViewChanged -= ScrollViewer_ViewChanged;
    }
}
