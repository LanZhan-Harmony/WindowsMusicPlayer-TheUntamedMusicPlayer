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

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayListDetailViewModel
    : ObservableRecipient,
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
        Messenger.Register(this);
        SongList = Playlist.SongList;
        IsPlayAllButtonEnabled = SongList.Count > 0;
    }

    public void Receive(PlaylistChangeMessage message)
    {
        Playlist = Data.SelectedPlaylist = message.Playlist;
        PlaylistName = Playlist.Name;
        TotalSongNumStr = Playlist.TotalSongNumStr;
        Cover = Playlist.Cover;
        SongList = Playlist.SongList;
        IsPlayAllButtonEnabled = SongList.Count > 0;
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (SongList.Count == 0)
        {
            return;
        }
        var songList = SongList.Select(s => s.Song).ToList();
        Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}", songList, 0, 0);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        Data.SelectedPlaylist = null;
        Data.ShellPage!.GetFrame().GoBack();
        Data.PlaylistLibrary.DeletePlaylist(Playlist);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var songList = SongList.Select(s => s.Song);
        Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{Playlist.Name}", songList, 0, 0);
        if (e.ClickedItem is IndexedPlaylistSong indexedInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(indexedInfo.Song);
        }
    }

    public void PlayButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.AddSongToNextPlay(info);
    }

    public void RemoveButton_Click(IndexedPlaylistSong info)
    {
        Data.PlaylistLibrary.DeleteFromPlaylist(Playlist, info);
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

    public void Dispose() => Messenger.Unregister<PlaylistChangeMessage>(this);
}
