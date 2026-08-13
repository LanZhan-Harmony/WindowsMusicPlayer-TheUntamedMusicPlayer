using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineAlbumsViewModel
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;

    public OnlineAlbumsViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
    }

    [RelayCommand]
    public async Task PlayButton(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        _musicPlayer.QueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
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
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Album:{info.Name}",
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
    public async Task AddToPlayQueueButton(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(info);
        var songList = detailedInfo.SongList;
        if (songList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Album:{info.Name}",
                songList
            );
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(songList);
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
                NavigationTransition.Suppress
            );
        }
    }
}
