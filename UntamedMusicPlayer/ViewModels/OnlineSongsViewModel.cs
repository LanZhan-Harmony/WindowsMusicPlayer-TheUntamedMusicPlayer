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

public sealed partial class OnlineSongsViewModel : OnlineSearchViewModelBase
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;
    private readonly CloudMusicApiService _cloudApi;

    public OnlineSongsViewModel(
        MusicPlayer musicPlayer,
        OnlineMusicLibrary onlineLibrary,
        CloudMusicApiService cloudApi
    )
        : base(onlineLibrary)
    {
        _musicPlayer = musicPlayer;
        _cloudApi = cloudApi;
    }

    public void OnlineSongsSongListView_ItemClick(IBriefOnlineSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:{OnlineLibrary.SearchKeyWords}",
            OnlineLibrary.SongSearchState.Items
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void OnlineSongsPlayButton(IBriefOnlineSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:{OnlineLibrary.SearchKeyWords}",
            OnlineLibrary.SongSearchState.Items
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void OnlineSongsPlayNextButton(IBriefOnlineSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]
    public void AddToPlayQueueButton(IBriefOnlineSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(Tuple<IBriefOnlineSongInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]
    public async Task ShowAlbumButton(IBriefOnlineSongInfo info)
    {
        var onlineAlbumInfo = await CloudMusicModelFactory.CreateAlbumFromSongAsync(
            info,
            _cloudApi
        );
        if (onlineAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineAlbumDetailPage),
                new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(OnlineSongsPage)),
                NavigationTransition.Suppress
            );
        }
    }

    [RelayCommand]
    public async Task ShowArtistButton(IBriefOnlineSongInfo info)
    {
        var onlineArtistInfo = await CloudMusicModelFactory.CreateArtistFromSongAsync(
            info,
            _cloudApi
        );
        if (onlineArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineArtistDetailPage),
                new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(OnlineSongsPage)),
                NavigationTransition.Suppress
            );
        }
    }

    // Legacy click-handler adapters kept for batch-refactor compatibility.
}
