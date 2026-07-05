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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
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
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay(songList);
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
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(songList);
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

