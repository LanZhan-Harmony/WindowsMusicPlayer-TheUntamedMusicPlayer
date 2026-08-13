using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlinePlayListDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;

    private IBriefOnlinePlaylistInfo? _cachedBriefPlaylist = null;
    public IBriefOnlinePlaylistInfo BriefPlaylist { get; set; } = null!;

    [ObservableProperty]
    public partial IDetailedOnlinePlaylistInfo Playlist { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    public OnlinePlayListDetailViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
    }

    public async Task Initialize(IBriefOnlinePlaylistInfo briefPlaylist)
    {
        BriefPlaylist = briefPlaylist;
        if (ShouldReloadPlaylist())
        {
            await LoadPlaylistAsync();
            _cachedBriefPlaylist = BriefPlaylist;
        }
        else
        {
            IsSearchProgressRingActive = false;
        }
    }

    private bool ShouldReloadPlaylist()
    {
        if (_cachedBriefPlaylist is null || Playlist is null)
        {
            return true;
        }
        if (_cachedBriefPlaylist.ID != BriefPlaylist.ID)
        {
            return true;
        }
        return false;
    }

    private async Task LoadPlaylistAsync()
    {
        Playlist = null!;
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        Playlist = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            BriefPlaylist
        );
        IsPlayAllButtonEnabled = Playlist.SongList.Count > 0;
        IsSearchProgressRingActive = false;
    }

    [RelayCommand]
    private void PlayAll()
    {
        if (Playlist.SongList.Count == 0)
        {
            return;
        }
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        _musicPlayer.PlaySongByInfo(Playlist.SongList[0]);
    }

    [RelayCommand]
    private void AddAllToPlayQueue()
    {
        if (Playlist.SongList.Count == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}",
                Playlist.SongList
            );
            _musicPlayer.PlaySongByInfo(Playlist.SongList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(Playlist.SongList);
        }
    }

    [RelayCommand]
    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)
    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, Playlist.SongList);
    }

    [RelayCommand]
    public void AddToPlayQueueFlyoutButton()
    {
        AddAllToPlayQueueCommand.Execute(null);
    }

    public void SongListView_ItemClick(IBriefOnlineSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void PlayButton(IBriefOnlineSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void PlayNextButton(IBriefOnlineSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}:Part",
                list
            );
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
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}:Part",
                list
            );
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
        var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(info);
        if (onlineAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineAlbumDetailPage),
                new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(OnlinePlayListDetailPage)),
                NavigationTransition.Suppress
            );
        }
    }

    [RelayCommand]
    public async Task ShowArtistButton(IBriefOnlineSongInfo info)
    {
        var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(info);
        if (onlineArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineArtistDetailPage),
                new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(OnlinePlayListDetailPage)),
                NavigationTransition.Suppress
            );
        }
    }
}
