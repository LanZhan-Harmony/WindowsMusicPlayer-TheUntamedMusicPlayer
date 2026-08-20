using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using ZLinq;

namespace UntamedMusicPlayer.Views;

public sealed partial class OnlineSongsPage : Page
{
    public OnlineSongsViewModel ViewModel { get; set; }
    private bool _isInitialized = false;
    private ScrollViewer? _scrollViewer;
    private bool _isSearching;

    public OnlineSongsPage()
    {
        ViewModel = App.GetService<OnlineSongsViewModel>();
        InitializeComponent();
    }

    private void OnlineSongsSongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            ViewModel.OnlineSongsSongListView_ItemClick(info);
        }
    }

    public Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IBriefOnlineSongInfo info } menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
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
            ViewModel.AddToPlaylistButtonCommand.Execute(Tuple.Create(songInfo, playlist));
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
            !_isInitialized
            && App.GetService<MusicPlayer>().State.CurrentBriefSong
                is IBriefOnlineSongInfo currentSong
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
        _isInitialized = true;
    }

    private async void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (
            !_isSearching
            && ViewModel.OnlineLibrary.SongSearchState.HasMore
            && _scrollViewer!.VerticalOffset + _scrollViewer.ViewportHeight
                >= _scrollViewer.ExtentHeight - 50
        )
        {
            _isSearching = true;
            await ViewModel.SearchMoreAsync();
            _isSearching = false;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.OnlineSongsPlayButtonCommand.Execute(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.OnlineSongsPlayNextButtonCommand.Execute(info);
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            await DownloadHelper.DownloadOnlineSongAsync(
                info,
                App.GetService<CloudMusicApiService>()
            );
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.AddToPlayQueueButtonCommand.Execute(info);
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
                ViewModel.AddToPlaylistButtonCommand.Execute(
                    Tuple.Create(info, dialog.CreatedPlaylist)
                );
            }
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            var song = await CloudMusicModelFactory.CreateDetailedSongAsync(
                info,
                App.GetService<CloudMusicApiService>()
            );
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.ShowAlbumButtonCommand.Execute(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineSongInfo info })
        {
            ViewModel.ShowArtistButtonCommand.Execute(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }

    private void OnlineSongsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer?.ViewChanged -= ScrollViewer_ViewChanged;
    }
}
