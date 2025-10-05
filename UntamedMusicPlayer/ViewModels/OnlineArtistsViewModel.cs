using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public class OnlineArtistsViewModel
{
    public OnlineArtistsViewModel() { }

    public async void PlayButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public async void PlayNextButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    public async void AddToPlayQueueButton_Click(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(songList);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefOnlineArtistInfo info, PlaylistInfo playlist)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }
}
