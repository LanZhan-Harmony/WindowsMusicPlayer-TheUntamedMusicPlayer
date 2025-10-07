using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public class OnlinePlayListsViewModel
{
    public OnlinePlayListsViewModel() { }

    public async void PlayButton_Click(IBriefOnlinePlaylistInfo info)
    {
        var detailedInfo = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            info
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void PlayNextButton_Click(IBriefOnlinePlaylistInfo info)
    {
        var detailedInfo = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            info
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    public async void AddToPlayQueueButton_Click(IBriefOnlinePlaylistInfo info)
    {
        var detailedInfo = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            info
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    public async void AddToPlaylistButton_Click(
        IBriefOnlinePlaylistInfo info,
        PlaylistInfo playlist
    )
    {
        var detailedInfo = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            info
        );
        var songList = detailedInfo.SongList;
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }
}
