using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Constants;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;
using Windows.Storage;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly MusicPlayer _musicPlayer;

    public bool IsFirstLoaded { get; set; } = true;

    public string NavigatePage { get; set; } = null!;

    public string CurrentPage { get; set; } = null!;

    public PlaylistInfo? PrevPlaylistInfo { get; set; }

    public ShellViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
        _ = LoadAsync();
    }

    public async Task SetCurrentPageAsync(string pageName, bool isBackNavigation)
    {
        if (isBackNavigation)
        {
            PrevPlaylistInfo = null;
        }
        CurrentPage = pageName;
        await SaveCurrentPageAsync();
    }

    public bool CanAcceptExternalStorageItems() => CurrentPage != nameof(PlayQueuePage);

    public async Task AddExternalStorageItemsToPlayQueueAsync(
        IReadOnlyList<IStorageItem> storageItems
    )
    {
        if (!CanAcceptExternalStorageItems())
        {
            return;
        }

        var musicFiles = new List<StorageFile>();
        foreach (var item in storageItems)
        {
            if (item is StorageFile file)
            {
                var extension = Path.GetExtension(file.Path).ToLowerInvariant();
                if (AppConstants.SupportedAudioTypes.Contains(extension))
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

        if (musicFiles.Count > 0)
        {
            await AddExternalFilesToPlayQueueAsync(musicFiles);
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
                if (AppConstants.SupportedAudioTypes.Contains(extension))
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

    public async Task AddExternalFilesToPlayQueueAsync(IReadOnlyList<StorageFile> files)
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
            _musicPlayer
                .QueueManager.SetNormalPlayQueue("LocalSongs:Part", newSongs);
            await _musicPlayer.PlaySongByInfoAsync(newSongs[0]);
        }
    }

    private async Task LoadAsync()
    {
        CurrentPage =
            await _localSettingsService.ReadSettingAsync<string>("CurrentPage") ?? nameof(HomePage);
    }

    private async Task SaveCurrentPageAsync()
    {
        await _localSettingsService.SaveSettingAsync("CurrentPage", CurrentPage);
    }
}
