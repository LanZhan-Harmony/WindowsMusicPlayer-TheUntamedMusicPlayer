using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public bool IsFirstLoaded { get; set; } = true;

    public string NavigatePage { get; set; } = null!;

    public string CurrentPage { get; set; } = null!;

    public PlaylistInfo? PrevPlaylistInfo { get; set; }

    [ObservableProperty]
    public partial object SelectedItem { get; set; } = null!;

    public ShellViewModel()
    {
        Data.ShellViewModel = this;
        LoadAsync();
    }

    public void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == NavigationMode.Back)
        {
            PrevPlaylistInfo = null;
        }
        CurrentPage = e.SourcePageType.Name;
        var navView = Data.ShellPage!.GetNavigationView();
        if (
            CurrentPage
            is nameof(HomePage)
                or nameof(OnlineAlbumDetailPage)
                or nameof(OnlineArtistDetailPage)
                or nameof(OnlinePlayListDetailPage)
        )
        {
            SelectedItem = navView.MenuItems[0];
        }
        else if (
            CurrentPage
            is nameof(MusicLibraryPage)
                or nameof(LocalAlbumDetailPage)
                or nameof(LocalArtistDetailPage)
        )
        {
            SelectedItem = navView.MenuItems[1];
        }
        else if (CurrentPage is nameof(PlayQueuePage))
        {
            SelectedItem = navView.MenuItems[3];
        }
        else if (CurrentPage is nameof(PlayListsPage))
        {
            SelectedItem = navView.MenuItems[4];
        }
        else if (CurrentPage is nameof(PlayListDetailPage))
        {
            var playlistsNavItem = navView.MenuItems[4] as NavigationViewItem;
            var playlistSubItem = playlistsNavItem!
                .MenuItems.AsValueEnumerable()
                .Cast<NavigationViewItem>()
                .FirstOrDefault(item =>
                    item.DataContext is PlaylistInfo playlist && playlist == PrevPlaylistInfo
                );
            if (playlistSubItem is not null)
            {
                SelectedItem = playlistSubItem;
            }
            else
            {
                SelectedItem = playlistsNavItem;
            }
        }
        else if (CurrentPage is nameof(SettingsPage))
        {
            SelectedItem = navView.FooterMenuItems[0];
        }
        SaveCurrentPageAsync();
    }

    public void NavigationFrame_DragOver(object sender, DragEventArgs e)
    {
        if (CurrentPage == nameof(PlayQueuePage))
        {
            return;
        }
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Shell_PlayFiles".GetLocalized();
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
        }
    }

    public async void NavigationFrame_Drop(object sender, DragEventArgs e)
    {
        if (CurrentPage == nameof(PlayQueuePage))
        {
            return;
        }
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var def = e.GetDeferral();
            var items = await e.DataView.GetStorageItemsAsync();
            var musicFiles = new List<StorageFile>();
            await Task.Run(async () =>
            {
                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        var extension = Path.GetExtension(file.Path).ToLowerInvariant();
                        if (Data.SupportedAudioTypes.Contains(extension))
                        {
                            musicFiles.Add(file);
                        }
                    }
                    else if (item is StorageFolder folder)
                    {
                        var folderFiles = await GetMusicFilesFromFolderAsync(folder);
                        musicFiles.AddRange(folderFiles);
                    }
                }
            });

            if (musicFiles.Count > 0)
            {
                await AddExternalFilesToPlayQueue(musicFiles);
            }
            def.Complete();
        }
    }

    private static async Task<List<StorageFile>> GetMusicFilesFromFolderAsync(StorageFolder folder)
    {
        var musicFiles = new List<StorageFile>();
        try
        {
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.Path).ToLowerInvariant();
                if (Data.SupportedAudioTypes.Contains(extension))
                {
                    musicFiles.Add(file);
                }
            }

            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
            {
                var subFiles = await GetMusicFilesFromFolderAsync(subFolder);
                musicFiles.AddRange(subFiles);
            }
        }
        catch { }
        return musicFiles;
    }

    public static async Task AddExternalFilesToPlayQueue(List<StorageFile> files)
    {
        var newSongs = new List<IBriefSongInfoBase>();
        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                try
                {
                    var folder = Path.GetDirectoryName(file.Path) ?? "";
                    var songInfo = new BriefLocalSongInfo(file.Path, folder);
                    if (songInfo.IsPlayAvailable)
                    {
                        newSongs.Add(songInfo);
                    }
                }
                catch { }
            }
        });
        if (newSongs.Count > 0)
        {
            Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", newSongs);
            Data.MusicPlayer.PlaySongByInfo(newSongs[0]);
        }
    }

    private async void LoadAsync()
    {
        CurrentPage =
            await _localSettingsService.ReadSettingAsync<string>("CurrentPage") ?? nameof(HomePage);
    }

    private async void SaveCurrentPageAsync()
    {
        await _localSettingsService.SaveSettingAsync("CurrentPage", CurrentPage);
    }
}
