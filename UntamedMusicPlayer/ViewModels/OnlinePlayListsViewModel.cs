using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlinePlayListsViewModel
{
    private readonly MusicPlayer _musicPlayer;

    public OnlinePlayListsViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
    }

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
        _musicPlayer
            .QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
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
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer
                .QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay(songList);
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
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer
                .QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(songList);
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
