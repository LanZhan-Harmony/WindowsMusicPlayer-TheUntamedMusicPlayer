using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Constants;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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
        var currentSong = App.GetService<MusicPlayer>().State.CurrentBriefSong;
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

    private void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IndexedPlayQueueSong info)
        {
            ViewModel.PlayCommand.Execute(info);
        }
    }

    private void PlayQueueListView_DragItemsStarting(
        object sender,
        DragItemsStartingEventArgs e
    )
    {
        ViewModel.BeginPlayQueueReorder();
        if (e.Items.Count > 0)
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void PlayQueueListView_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "PlayQueue_AddToPlayQueue".GetLocalized();
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
        }
    }

    private void PlayQueueListView_DragItemsCompleted(
        object sender,
        DragItemsCompletedEventArgs args
    )
    {
        if (args.DropResult == DataPackageOperation.Move && args.Items.Count > 0)
        {
            var songs = args.Items.AsValueEnumerable().OfType<IndexedPlayQueueSong>().ToArray();
            ViewModel.CompletePlayQueueReorder(songs);
        }
    }

    private async void PlayQueueListView_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var musicFilePaths = await GetMusicFilePathsAsync(items);
            if (musicFilePaths.Count == 0 || sender is not ListView listView)
            {
                return;
            }

            await ViewModel.AddExternalFilesToPlayQueueAsync(
                musicFilePaths,
                GetPlayQueueDropIndex(listView, e)
            );
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static async Task<List<string>> GetMusicFilePathsAsync(
        IReadOnlyList<IStorageItem> items
    )
    {
        var musicFilePaths = new List<string>();
        foreach (var item in items)
        {
            if (item is StorageFile file)
            {
                AddIfSupportedAudioFile(musicFilePaths, file.Path);
            }
            else if (item is StorageFolder folder)
            {
                var folderFiles = await GetMusicFilePathsFromFolderAsync(folder);
                musicFilePaths.AddRange(folderFiles);
            }
        }

        return musicFilePaths;
    }

    private static async Task<List<string>> GetMusicFilePathsFromFolderAsync(StorageFolder folder)
    {
        var musicFilePaths = new List<string>();
        try
        {
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                AddIfSupportedAudioFile(musicFilePaths, file.Path);
            }

            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
            {
                var subFiles = await GetMusicFilePathsFromFolderAsync(subFolder);
                musicFilePaths.AddRange(subFiles);
            }
        }
        catch { }

        return musicFilePaths;
    }

    private static void AddIfSupportedAudioFile(List<string> musicFilePaths, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (AppConstants.SupportedAudioTypes.Contains(extension))
        {
            musicFilePaths.Add(path);
        }
    }

    private static int GetPlayQueueDropIndex(ListView listView, DragEventArgs e)
    {
        if (listView.Items.Count == 0)
        {
            return 0;
        }

        UIElement relativeTarget =
            listView.ItemsPanelRoot is not null ? listView.ItemsPanelRoot : listView;
        var position = e.GetPosition(relativeTarget);
        if (listView.ContainerFromIndex(0) is not ListViewItem sampleItem)
        {
            return listView.Items.Count;
        }

        var itemHeight = sampleItem.ActualHeight + sampleItem.Margin.Top + sampleItem.Margin.Bottom;
        if (itemHeight <= 0)
        {
            return listView.Items.Count;
        }

        var calculatedIndex = (int)(position.Y / itemHeight);
        return Math.Min(listView.Items.Count, Math.Max(0, calculatedIndex));
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
            var isCurrentlyPlaying = App.GetService<MusicPlayer>().State.PlayQueueIndex == songInfo.Index;
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






