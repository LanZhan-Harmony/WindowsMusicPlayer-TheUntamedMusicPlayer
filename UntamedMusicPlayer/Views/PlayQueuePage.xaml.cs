using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;
using ZLinq;

namespace UntamedMusicPlayer.Views;

public sealed partial class PlayQueuePage : Page
{
    public PlayQueueViewModel ViewModel { get; }
    private bool _isInitialized = false;

    public PlayQueuePage()
    {
        ViewModel = App.GetService<PlayQueueViewModel>();
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
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
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
            ViewModel.AddToPlaylistCommand.ExecuteAsync(
                new Tuple<IBriefSongInfoBase, PlaylistInfo>(songInfo, playlist)
            );
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
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
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
            ViewModel.AddQueueToPlaylistCommand.ExecuteAsync(playlist);
        }
    }

    private void AddToPlayQueueFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddQueueToPlayQueueCommand.Execute(null);
    }

    private async void AddToNewPlaylistFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            await ViewModel.AddQueueToPlaylistCommand.ExecuteAsync(dialog.CreatedPlaylist);
        }
    }

    private async void PlayQueueListView_Loaded(object sender, RoutedEventArgs e)
    {
        await App.GetService<MusicPlayer>().WhenLoadedAsync();
        var currentSong = Data.PlayState.CurrentBriefSong;
        if (currentSong is null)
        {
            return;
        }

        var listView = (sender as ListView)!;
        var listViewSource = listView.ItemsSource;
        if (!_isInitialized && listViewSource is IEnumerable<IndexedPlayQueueSong> songs)
        {
            var targetSong = songs
                .AsValueEnumerable()
                .FirstOrDefault(song => SongComparer.IsSameSong(song.Song, currentSong));
            if (targetSong is not null)
            {
                listView.ScrollIntoView(targetSong, ScrollIntoViewAlignment.Leading);
            }
        }
        _isInitialized = true;
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
            var isCurrentlyPlaying = Data.PlayState.PlayQueueIndex == songInfo.Index;
            playingFontIcon.Visibility = isCurrentlyPlaying
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.PlayCommand.Execute(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.PlayNextCommand.Execute(info.Song);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.AddToPlayQueueCommand.Execute(info.Song);
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
                await ViewModel.AddToPlaylistCommand.ExecuteAsync(
                    new Tuple<IBriefSongInfoBase, PlaylistInfo>(info.Song, dialog.CreatedPlaylist)
                );
            }
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.RemoveCommand.Execute(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.MoveUpCommand.Execute(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.MoveDownCommand.Execute(info);
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
            ViewModel.ShowAlbumCommand.ExecuteAsync(info.Song);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IndexedPlayQueueSong info })
        {
            ViewModel.ShowArtistCommand.ExecuteAsync(info.Song);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }

    private async void AddFilesSplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        sender.IsEnabled = false;
        await ViewModel.AddFilesCommand.ExecuteAsync(null);
        sender.IsEnabled = true;
    }

    private async void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        AddFilesSplitButton.IsEnabled = false;
        await ViewModel.AddFilesCommand.ExecuteAsync(null);
        AddFilesSplitButton.IsEnabled = true;
    }

    private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        AddFilesSplitButton.IsEnabled = false;
        await ViewModel.AddFolderCommand.ExecuteAsync(null);
        AddFilesSplitButton.IsEnabled = true;
    }

    private async void AddUrlButton_Click(object sender, RoutedEventArgs e)
    {
        AddFilesSplitButton.IsEnabled = false;
        AddFilesSplitButton.Flyout.Hide();
        var contentTextBox = new TextBox
        {
            PlaceholderText = "PlayQueue_AddUrlDialog_EnterTheURL".GetLocalized(),
            Width = 290,
        };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = Application.Current.Resources["NormalContentDialogStyle"] as Style,
            RequestedTheme = ThemeSelectorService.IsDarkTheme
                ? ElementTheme.Dark
                : ElementTheme.Light,
            Title = new TextBlock { Text = "PlayQueue_AddUrlDialog_OpenAURL".GetLocalized() },
            Content = contentTextBox,
            PrimaryButtonText = "PlayQueue_AddUrlDialog_Open".GetLocalized(),
            IsPrimaryButtonEnabled = false,
            CloseButtonText = "PlayQueue_AddUrlDialog_Cancel".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
        };
        dialog.EnableLightDismiss();
        contentTextBox.TextChanged += (sender, _) =>
        {
            dialog.IsPrimaryButtonEnabled = Uri.TryCreate(
                (sender as TextBox)!.Text,
                UriKind.Absolute,
                out var _
            );
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddUrlCommand.Execute(contentTextBox.Text);
        }
        AddFilesSplitButton.IsEnabled = true;
    }
}






