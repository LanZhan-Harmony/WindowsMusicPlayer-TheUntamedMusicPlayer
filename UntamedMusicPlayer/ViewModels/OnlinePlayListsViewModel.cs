using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlinePlayListsViewModel : OnlineSearchViewModelBase
{
    private readonly MusicPlayer _musicPlayer;
    private readonly CloudMusicApiService _cloudApi;

    public OnlinePlayListsViewModel(
        MusicPlayer musicPlayer,
        OnlineMusicLibrary onlineLibrary,
        CloudMusicApiService cloudApi
    )
        : base(onlineLibrary)
    {
        _musicPlayer = musicPlayer;
        _cloudApi = cloudApi;
    }

    [RelayCommand]
    private async Task Play(IBriefOnlinePlaylistInfo info)
    {
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedPlaylistAsync(
            info,
            _cloudApi
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        _musicPlayer.QueueManager.SetNormalPlayQueue($"OnlineSongs:Playlist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    private async Task PlayNext(IBriefOnlinePlaylistInfo info)
    {
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedPlaylistAsync(
            info,
            _cloudApi
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{info.Name}",
                songList
            );
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
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedPlaylistAsync(
            info,
            _cloudApi
        );
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{info.Name}",
                songList
            );
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
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedPlaylistAsync(
            info,
            _cloudApi
        );
        var songList = detailedInfo.SongList;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }
}
