using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public class OnlineAlbumsViewModel
{
    public OnlineAlbumsViewModel() { }

    public async void PlayButton_Click(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void PlayNextButton_Click(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    public async void AddToPlayQueueButton_Click(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(songList);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefOnlineAlbumInfo info, PlaylistInfo playlist)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public async void ShowArtistButton_Click(IBriefOnlineAlbumInfo info)
    {
        var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromAlbumInfoAsync(info);
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
