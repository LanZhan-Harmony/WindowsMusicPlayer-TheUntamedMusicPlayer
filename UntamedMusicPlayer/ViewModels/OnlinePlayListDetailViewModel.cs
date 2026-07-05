using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlinePlayListDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    private IBriefOnlinePlaylistInfo? _cachedBriefPlaylist = null;
    public IBriefOnlinePlaylistInfo BriefPlaylist { get; set; } = null!;

    [ObservableProperty]
    public partial IDetailedOnlinePlaylistInfo Playlist { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    public OnlinePlayListDetailViewModel() { }

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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(Playlist.SongList[0]);
    }


    [RelayCommand]
    private void AddAllToPlayQueue()
    {
        if (Playlist.SongList.Count == 0)
        {
            return;
        }
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}",
                Playlist.SongList
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(Playlist.SongList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(Playlist.SongList);
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

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
    }

    [RelayCommand]

    public void PlayButton(IBriefOnlineSongInfo info)

    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Playlist:{Playlist.Name}",
            Playlist.SongList
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void PlayNextButton(IBriefOnlineSongInfo info)

    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}:Part",
                list
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]

    public void AddToPlayQueueButton(IBriefOnlineSongInfo info)

    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Playlist:{Playlist.Name}:Part",
                list
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd([info]);
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
                new OnlineAlbumNavigationArgs(
                    onlineAlbumInfo,
                    nameof(OnlinePlayListDetailPage)
                ),
                new SuppressNavigationTransitionInfo()
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
                new OnlineArtistNavigationArgs(
                    onlineArtistInfo,
                    nameof(OnlinePlayListDetailPage)
                ),
                new SuppressNavigationTransitionInfo()
            );
        }
    }








}

