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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
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
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay(songList);
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
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(songList);
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

