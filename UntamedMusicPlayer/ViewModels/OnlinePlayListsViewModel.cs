using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlinePlayListsViewModel
{
    public OnlinePlayListsViewModel() { }

    [RelayCommand]
    private async Task Play(IBriefOnlinePlaylistInfo info)
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
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    private async Task PlayNext(IBriefOnlinePlaylistInfo info)
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
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]
    private async Task AddToPlayQueue(IBriefOnlinePlaylistInfo info)
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
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]
    private async Task AddToPlaylist(Tuple<IBriefOnlinePlaylistInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        var detailedInfo = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            info
        );
        var songList = detailedInfo.SongList;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }
}

