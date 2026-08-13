using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineArtistsViewModel
{
    private readonly MusicPlayer _musicPlayer;

    public OnlineArtistsViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
    }

    [RelayCommand]
    public async Task PlayButton(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        _musicPlayer
            .QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    public async Task PlayNextButton(IBriefOnlineArtistInfo info)
    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer
                .QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
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
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer
                .QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
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
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }
}
