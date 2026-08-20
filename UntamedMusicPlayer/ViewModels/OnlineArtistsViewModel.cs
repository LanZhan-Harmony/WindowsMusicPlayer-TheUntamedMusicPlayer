using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Playback;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineArtistsViewModel : OnlineSearchViewModelBase
{
    private readonly MusicPlayer _musicPlayer;
    private readonly CloudMusicApiService _cloudApi;

    public OnlineArtistsViewModel(
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
    public async Task PlayButton(IBriefOnlineArtistInfo info)
    {
        var songList = await CloudMusicModelFactory.GetSongsByArtistAsync(info, _cloudApi);
        if (songList.Count == 0)
        {
            return;
        }
        _musicPlayer.QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    public async Task PlayNextButton(IBriefOnlineArtistInfo info)
    {
        var songList = await CloudMusicModelFactory.GetSongsByArtistAsync(info, _cloudApi);
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{info.Name}",
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
    public async Task AddToPlayQueueButton(IBriefOnlineArtistInfo info)
    {
        var songList = await CloudMusicModelFactory.GetSongsByArtistAsync(info, _cloudApi);
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{info.Name}",
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
    public async Task AddToPlaylistButton(Tuple<IBriefOnlineArtistInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        var songList = await CloudMusicModelFactory.GetSongsByArtistAsync(info, _cloudApi);
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }
}
