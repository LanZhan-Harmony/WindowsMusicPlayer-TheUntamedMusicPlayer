using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using Windows.ApplicationModel.DataTransfer;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayListDetailViewModel
    : ObservableRecipient,
        IRecipient<PlaylistRenameMessage>,
        IRecipient<PlaylistChangeMessage>,
        IDisposable
{
    [ObservableProperty]
    public partial PlaylistInfo Playlist { get; set; } = Data.SelectedPlaylist!;

    [ObservableProperty]
    public partial string PlaylistName { get; set; } = Data.SelectedPlaylist!.Name;

    [ObservableProperty]
    public partial string TotalSongNumStr { get; set; } = Data.SelectedPlaylist!.TotalSongNumStr;

    [ObservableProperty]
    public partial WriteableBitmap? Cover { get; set; } = Data.SelectedPlaylist!.Cover;

    [ObservableProperty]
    public partial ObservableCollection<IndexedPlaylistSong> SongList { get; set; }

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    public PlayListDetailViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register<PlaylistRenameMessage>(this);
        Messenger.Register<PlaylistChangeMessage>(this);
        SongList = Playlist.SongList;
        IsPlayAllButtonEnabled = SongList.Count > 0;
    }

    public void Receive(PlaylistRenameMessage message)
    {
        if (PlaylistName == message.OldName)
        {
            PlaylistName = message.NewName;
        }
    }

    public void Receive(PlaylistChangeMessage message)
    {
        if (PlaylistName == message.Playlist.Name)
        {
            TotalSongNumStr = Playlist.TotalSongNumStr;
            Cover = null;
            Cover = Playlist.Cover;
            SongList = Playlist.SongList;
            IsPlayAllButtonEnabled = SongList.Count > 0;
        }
    }

    public void PlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        if (SongList.Count == 0)
        {
            return;
        }
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        if (SongList.Count == 0)
        {
            return;
        }
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(songList);
        }
    }

    public void DeleteButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.SelectedPlaylist = null;
        Data.ShellPage!.GoBack();
        Data.PlaylistLibrary.DeletePlaylist(Playlist);
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        var songList = SongList.AsValueEnumerable().Select(s => s.Song).ToArray();
        Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}", songList);
        if (e.ClickedItem is IndexedPlaylistSong indexedInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(indexedInfo.Song);
        }
    }

    public void PlayButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"Songs:Playlist:{Playlist.Name}",
            SongList.AsValueEnumerable().Select(s => s.Song).ToArray()
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void AddToPlayQueueButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToPlayQueue(info);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefSongInfoBase info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public async void RemoveButton_Click(IndexedPlaylistSong info)
    {
        await Data.PlaylistLibrary.DeleteFromPlaylist(Playlist, info);
        if (SongList.Count == 0)
        {
            IsPlayAllButtonEnabled = false;
        }
    }

    public void MoveUpButton_Click(IndexedPlaylistSong info)
    {
        Data.PlaylistLibrary.MoveUpInPlaylist(Playlist, info);
    }

    public void MoveDownButton_Click(IndexedPlaylistSong info)
    {
        Data.PlaylistLibrary.MoveDownInPlaylist(Playlist, info);
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

    public void SongListView_DragItemsStarting(object _, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0)
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    public void SongListView_DragItemsCompleted(
        ListViewBase _1,
        DragItemsCompletedEventArgs args
    )
    {
        if (args.DropResult == DataPackageOperation.Move && args.Items.Count > 0)
        {
            var songs = args.Items.AsValueEnumerable().OfType<IndexedPlaylistSong>().ToArray();
            if (songs.Length == 0)
            {
                return;
            }
            Playlist.ReindexSongs();
            Messenger.Send(new HavePlaylistMessage(true));
            _ = FileManager.SavePlaylistDataAsync(Data.PlaylistLibrary.Playlists);
        }
    }

    public void Dispose()
    {
        Messenger.Unregister<PlaylistRenameMessage>(this);
        Messenger.Unregister<PlaylistChangeMessage>(this);
    }
}
