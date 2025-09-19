using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.Storage.Pickers;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayQueueViewModel : ObservableObject
{
    private IndexedPlayQueueSong? _currentSong;

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlayQueueSong> PlayQueue { get; set; } =
        Data.MusicPlayer.ShuffleMode
            ? Data.MusicPlayer.ShuffledPlayQueue
            : Data.MusicPlayer.PlayQueue;

    [ObservableProperty]
    public partial bool IsButtonEnabled { get; set; } = false;

    public PlayQueueViewModel()
    {
        IsButtonEnabled = PlayQueue.Count > 0;
        Data.MusicPlayer.PropertyChanged += MusicPlayer_PropertyChanged;
    }

    private void MusicPlayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName == nameof(Data.MusicPlayer.ShuffleMode)
            || e.PropertyName == nameof(Data.MusicPlayer.PlayQueue)
            || e.PropertyName == nameof(Data.MusicPlayer.ShuffledPlayQueue)
        )
        {
            PlayQueue = Data.MusicPlayer.ShuffleMode
                ? Data.MusicPlayer.ShuffledPlayQueue
                : Data.MusicPlayer.PlayQueue;
            IsButtonEnabled = PlayQueue.Count > 0;
        }
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        var songList = PlayQueue.AsValueEnumerable().Select(song => song.Song).ToArray();
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        var songList = PlayQueue.AsValueEnumerable().Select(song => song.Song).ToArray();
        Data.MusicPlayer.AddSongsToPlayQueue(songList);
    }

    public void PlayQueueListView_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IndexedPlayQueueSong info)
        {
            Data.MusicPlayer.PlaySongByIndexedInfo(info);
        }
    }

    public void PlayButton_Click(IndexedPlayQueueSong info)
    {
        Data.MusicPlayer.PlaySongByIndexedInfo(info);
    }

    public void PlayNextButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.AddSongToNextPlay(info);
    }

    public void AddToPlayQueueButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.AddSongToPlayQueue(info);
    }

    public async void AddToPlaylistButton_Click(IBriefSongInfoBase info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public async void RemoveButton_Click(IndexedPlayQueueSong info)
    {
        await Data.MusicPlayer.RemoveSong(info);
    }

    public void MoveUpButton_Click(IndexedPlayQueueSong info)
    {
        Data.MusicPlayer.MoveUpSong(info);
    }

    public void MoveDownButton_Click(IndexedPlayQueueSong info)
    {
        Data.MusicPlayer.MoveDownSong(info);
    }

    public async void ShowAlbumButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                Data.SelectedLocalAlbum = localAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                Data.SelectedOnlineAlbum = onlineAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    public async void ShowArtistButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                Data.SelectedLocalArtist = localArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalArtistDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                Data.SelectedOnlineArtist = onlineArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineArtistDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    public void ClearButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.MusicPlayer.ClearPlayQueue();
    }

    public async Task AddFilesButton_Click()
    {
        var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        Array.ForEach(Data.SupportedAudioTypes, picker.FileTypeFilter.Add);
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await AddExternalFilesToPlayQueue(
                [.. files.AsValueEnumerable().Select(f => f.Path)],
                PlayQueue.Count
            );
        }
    }

    public async Task AddFolderButton_Click()
    {
        var picker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            List<StorageFile>? musicFiles = null;
            await Task.Run(async () =>
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(folder.Path);
                musicFiles = await GetMusicFilesFromFolderAsync(storageFolder);
            });
            if (musicFiles?.Count > 0)
            {
                await AddExternalFilesToPlayQueue(
                    [.. musicFiles.AsValueEnumerable().Select(f => f.Path)],
                    PlayQueue.Count
                );
            }
        }
    }

    public void AddUrlButton_Click(string url)
    {
        var songInfo = new BriefUnknownSongInfo(new Uri(url));
        if (!songInfo.IsPlayAvailable)
        {
            return;
        }
        if (PlayQueue.Count > 0)
        {
            Data.MusicPlayer.AddSongToPlayQueue(songInfo);
        }
        else
        {
            Data.MusicPlayer.SetPlayQueue("UnknownOnlineSongs:Part", [songInfo]);
            Data.MusicPlayer.PlaySongByInfo(songInfo);
        }
        IsButtonEnabled = PlayQueue.Count > 0;
    }

    public void PlayQueueListView_DragItemsStarting(object _, DragItemsStartingEventArgs e)
    {
        _currentSong = PlayQueue[Data.MusicPlayer.PlayQueueIndex];
        if (e.Items.Count > 0)
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    public void PlayQueueListView_DragOver(object _, DragEventArgs e)
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

    public void PlayQueueListView_DragItemsCompleted(
        object _,
        DragItemsCompletedEventArgs args
    )
    {
        // 检查是否是重排序操作（Move操作且在同一个ListView内）
        if (args.DropResult == DataPackageOperation.Move && args.Items.Count > 0)
        {
            var songs = args.Items.AsValueEnumerable().Cast<IndexedPlayQueueSong>().ToArray();
            if (songs.Length == 0)
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
            Data.MusicPlayer.PlayQueueIndex = _currentSong!.Index;
        }
    }

    public async void PlayQueueListView_Drop(object sender, DragEventArgs e)
    {
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
                var listView = (ListView)sender;
                var position = e.GetPosition(listView.ItemsPanelRoot);
                var index = 0;

                if (listView.Items.Count > 0)
                {
                    var sampleItem = (ListViewItem)listView.ContainerFromIndex(0);
                    var itemHeight =
                        sampleItem.ActualHeight + sampleItem.Margin.Top + sampleItem.Margin.Bottom;

                    if (itemHeight > 0)
                    {
                        var calculatedIndex = (int)(position.Y / itemHeight);
                        index =
                            calculatedIndex >= listView.Items.Count
                                ? listView.Items.Count
                                : calculatedIndex;
                        index = Math.Min(listView.Items.Count, Math.Max(0, index));
                    }
                }

                await AddExternalFilesToPlayQueue(
                    [.. musicFiles.AsValueEnumerable().Select(f => f.Path)],
                    index
                );
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

    public async Task AddExternalFilesToPlayQueue(List<string> files, int insertIndex)
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
                Data.MusicPlayer.InsertSongsToPlayQueue(newSongs, insertIndex);
            }
            else
            {
                Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", newSongs);
                Data.MusicPlayer.PlaySongByInfo(newSongs[0]);
            }
        }
        IsButtonEnabled = PlayQueue.Count > 0;
    }
}
