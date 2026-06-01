using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineAlbumsViewModel
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    public OnlineAlbumsViewModel() { }

    [RelayCommand]

    public async Task PlayButton(IBriefOnlineAlbumInfo info)

    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public async Task PlayNextButton(IBriefOnlineAlbumInfo info)

    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]

    public async Task AddToPlayQueueButton(IBriefOnlineAlbumInfo info)

    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]

    public async Task AddToPlaylistButton(Tuple<IBriefOnlineAlbumInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    [RelayCommand]

    public async Task ShowArtistButton(IBriefOnlineAlbumInfo info)

    {
        var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromAlbumInfoAsync(info);
        if (onlineArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineArtistDetailPage),
                new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(OnlineAlbumsPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }





}

