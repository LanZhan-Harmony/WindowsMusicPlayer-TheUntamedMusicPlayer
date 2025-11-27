using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed class OnlineAlbumsViewModel
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
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
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
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
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
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
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
