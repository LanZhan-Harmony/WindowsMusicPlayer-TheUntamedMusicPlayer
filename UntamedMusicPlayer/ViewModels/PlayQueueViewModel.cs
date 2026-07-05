using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Constants;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Views;
using Windows.Storage;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class PlayQueueViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer = App.GetService<MusicPlayer>();
    private readonly PlayQueueManager _playQueueManager;
    private readonly SharedPlaybackState _playState;

    private IndexedPlayQueueSong? _currentSong;

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> PlayQueue { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsButtonEnabled { get; set; } = false;

    public PlayQueueViewModel()
    {
        _playQueueManager = _musicPlayer.QueueManager;
        _playState = _musicPlayer.State;
        PlayQueue = _playQueueManager.CurrentQueue;
        IsButtonEnabled = PlayQueue.Count > 0;
        _playQueueManager.PropertyChanged += OnPlayQueueChanged;
    }

    private void OnPlayQueueChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayQueueManager.CurrentQueue))
        {
            PlayQueue = _playQueueManager.CurrentQueue;
            IsButtonEnabled = PlayQueue.Count > 0;
        }
    }

    [RelayCommand]
    private async Task AddQueueToPlaylist(PlaylistInfo playlist)
    {
        var songList = PlayQueue.AsValueEnumerable().Select(song => song.Song).ToArray();
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    [RelayCommand]
    private void AddQueueToPlayQueue()
    {
        var songList = PlayQueue.AsValueEnumerable().Select(song => song.Song).ToArray();
        _playQueueManager.AddSongsToEnd(songList);
    }

    [RelayCommand]
    private void Play(IndexedPlayQueueSong info)
    {
        _musicPlayer.PlaySongByIndexedInfo(info);
    }

    [RelayCommand]
    private void PlayNext(IBriefSongInfoBase info)
    {
        _playQueueManager.AddSongsToNextPlay([info]);
    }

    [RelayCommand]
    private void AddToPlayQueue(IBriefSongInfoBase info)
    {
        _playQueueManager.AddSongsToEnd([info]);
    }

    [RelayCommand]
    private async Task AddToPlaylist(Tuple<IBriefSongInfoBase, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]
    private void Remove(IndexedPlayQueueSong info)
    {
        _playQueueManager.RemoveSong(info);
    }

    [RelayCommand]
    private void MoveUp(IndexedPlayQueueSong info)
    {
        _playQueueManager.MoveUpSong(info);
    }

    [RelayCommand]
    private void MoveDown(IndexedPlayQueueSong info)
    {
        _playQueueManager.MoveDownSong(info);
    }

    [RelayCommand]
    private async Task ShowAlbum(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(localAlbumInfo, nameof(PlayQueuePage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineAlbumDetailPage),
                    new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(PlayQueuePage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    [RelayCommand]
    private async Task ShowArtist(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(LocalArtistDetailPage),
                    new LocalArtistNavigationArgs(localArtistInfo, nameof(PlayQueuePage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                _navigationService.NavigateShell(
                    nameof(OnlineArtistDetailPage),
                    new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(PlayQueuePage)),
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _musicPlayer.ClearPlayQueue();
    }

    [RelayCommand]
    public async Task AddFiles()
    {
        var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        Array.ForEach(AppConstants.SupportedAudioTypes, picker.FileTypeFilter.Add);
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await AddExternalFilesToPlayQueueAsync(
                [.. files.AsValueEnumerable().Select(f => f.Path)],
                PlayQueue.Count
            );
        }
    }

    [RelayCommand]
    public async Task AddFolder()
    {
        var picker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder.Path);
            var musicFilePaths = await GetMusicFilePathsFromFolderAsync(storageFolder);
            if (musicFilePaths.Count > 0)
            {
                await AddExternalFilesToPlayQueueAsync(musicFilePaths, PlayQueue.Count);
            }
        }
    }

    [RelayCommand]
    public void AddUrl(string url)
    {
        var songInfo = new BriefUnknownSongInfo(new Uri(url));
        if (!songInfo.IsPlayAvailable)
        {
            return;
        }
        if (PlayQueue.Count > 0)
        {
            _playQueueManager.AddSongsToEnd([songInfo]);
        }
        else
        {
            _playQueueManager.SetNormalPlayQueue("UnknownOnlineSongs:Part", [songInfo]);
            _musicPlayer.PlaySongByInfo(songInfo);
        }
        IsButtonEnabled = PlayQueue.Count > 0;
    }

    public void BeginPlayQueueReorder()
    {
        _currentSong = PlayQueue[_playState.PlayQueueIndex];
    }

    public void CompletePlayQueueReorder(IReadOnlyList<IndexedPlayQueueSong> songs)
    {
        if (songs.Count == 0)
        {
            return;
        }

        var oldIndex = songs[0].Index;
        var newIndex = PlayQueue.IndexOf(songs[0]);
        if (oldIndex == newIndex)
        {
            return;
        }

        for (var i = 0; i < PlayQueue.Count; i++)
        {
            PlayQueue[i].Index = i;
        }
        _playState.PlayQueueIndex = _currentSong!.Index;
    }

    public async Task AddExternalFilesToPlayQueueAsync(IReadOnlyList<string> files, int insertIndex)
    {
        var newSongs = new List<IBriefSongInfoBase>();
        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                try
                {
                    var folder = Path.GetDirectoryName(file) ?? "";
                    var songInfo = new BriefLocalSongInfo(file, folder);
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
            if (PlayQueue.Count > 0)
            {
                _playQueueManager.InsertSongsAt(newSongs, insertIndex);
            }
            else
            {
                _playQueueManager.SetNormalPlayQueue("LocalSongs:Part", newSongs);
                await _musicPlayer.PlaySongByInfoAsync(newSongs[0]);
            }
        }
        IsButtonEnabled = PlayQueue.Count > 0;
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

    public void Dispose()
    {
        _playQueueManager.PropertyChanged -= OnPlayQueueChanged;
    }
}

