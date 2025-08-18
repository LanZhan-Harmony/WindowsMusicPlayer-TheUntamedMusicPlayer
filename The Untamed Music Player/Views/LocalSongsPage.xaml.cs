using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalSongsPage : Page, IRecipient<ScrollToSongMessage>
{
    public LocalSongsViewModel ViewModel { get; }

    public LocalSongsPage()
    {
        ViewModel = App.GetService<LocalSongsViewModel>();
        StrongReferenceMessenger.Default.Register(this);
        InitializeComponent();
    }

    private void LocalSongsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.Unregister<ScrollToSongMessage>(this);
    }

    public void Receive(ScrollToSongMessage message)
    {
        if (message.Song is null)
        {
            return;
        }

        var listViewSource = SongListView.ItemsSource;
        IBriefSongInfoBase? targetSong = null;
        if (listViewSource is IEnumerable<BriefLocalSongInfo> songs)
        {
            targetSong = songs.FirstOrDefault(song => song.Path == message.Song.Path);
        }
        else if (listViewSource is ICollectionView groupedSongs)
        {
            targetSong = groupedSongs
                .OfType<BriefLocalSongInfo>()
                .FirstOrDefault(song => song.Path == message.Song.Path);
        }
        if (targetSong is not null)
        {
            SongListView.ScrollIntoView(targetSong, message.Alignment);
        }
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: BriefLocalSongInfo info } menuItem)
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
                    DataContext = new Tuple<BriefLocalSongInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<BriefLocalSongInfo, PlaylistInfo> tuple })
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

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.AddToPlayQueueButton_Click(info);
        }
    }

    private void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.AddToNewPlaylistButton_Click(info);
        }
    }

    private async void EditInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            var dialog = new EditSongInfoDialog(info) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            var song = new DetailedLocalSongInfo(info);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
