using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public sealed class OnlineArtistsViewModel
{
    public OnlineArtistsViewModel() { }

    public async void PlayButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void PlayNextButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    public async void AddToPlayQueueButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefOnlineArtistInfo info, PlaylistInfo playlist)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }
}
