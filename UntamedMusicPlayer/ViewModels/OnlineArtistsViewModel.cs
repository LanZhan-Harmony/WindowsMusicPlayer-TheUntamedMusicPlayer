using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineArtistsViewModel
{
    public OnlineArtistsViewModel() { }

    [RelayCommand]

    public async Task PlayButton(IBriefOnlineArtistInfo info)

    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public async Task PlayNextButton(IBriefOnlineArtistInfo info)

    {
        var songList = await IBriefOnlineArtistInfo.GetSongsByArtistAsync(info);
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
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
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
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

