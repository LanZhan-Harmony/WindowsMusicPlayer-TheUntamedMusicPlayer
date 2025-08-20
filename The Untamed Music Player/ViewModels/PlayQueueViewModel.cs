using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayQueueViewModel : ObservableObject
{
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
        var songList = PlayQueue.Select(song => song.Song);
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        var songList = PlayQueue.Select(song => song.Song).ToList();
        Data.MusicPlayer.AddSongsToPlayQueue(songList);
    }

    public void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
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

    public void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.ClearPlayQueue();
    }
}
