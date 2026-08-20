using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineAlbumsViewModel : OnlineSearchViewModelBase
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;
    private readonly CloudMusicApiService _cloudApi;

    public OnlineAlbumsViewModel(
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
    public async Task PlayButton(IBriefOnlineAlbumInfo info)
    {
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedAlbumAsync(info, _cloudApi);
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
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedAlbumAsync(info, _cloudApi);
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
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedAlbumAsync(info, _cloudApi);
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
        var detailedInfo = await CloudMusicModelFactory.CreateDetailedAlbumAsync(info, _cloudApi);
        var songList = detailedInfo.SongList;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    [RelayCommand]
    public async Task ShowArtistButton(IBriefOnlineAlbumInfo info)
    {
        var onlineArtistInfo = await CloudMusicModelFactory.CreateArtistFromAlbumAsync(
            info,
            _cloudApi
        );
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
