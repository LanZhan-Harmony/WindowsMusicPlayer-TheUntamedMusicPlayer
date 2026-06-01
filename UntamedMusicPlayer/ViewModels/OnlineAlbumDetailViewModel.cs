using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public partial class OnlineAlbumDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    private IBriefOnlineAlbumInfo? _cachedBriefAlbum = null;
    public IBriefOnlineAlbumInfo BriefAlbum { get; set; } = null!;

    [ObservableProperty]
    public partial IDetailedOnlineAlbumInfo Album { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    public OnlineAlbumDetailViewModel() { }

    public async Task Initialize(IBriefOnlineAlbumInfo briefAlbum)
    {
        BriefAlbum = briefAlbum;
        if (ShouldReloadAlbum())
        {
            await LoadAlbumAsync();
            _cachedBriefAlbum = BriefAlbum;
        }
        else
        {
            IsSearchProgressRingActive = false;
        }
    }

    private bool ShouldReloadAlbum()
    {
        if (_cachedBriefAlbum is null || Album is null)
        {
            return true;
        }
        if (_cachedBriefAlbum.ID != BriefAlbum.ID)
        {
            return true;
        }
        return false;
    }

    private async Task LoadAlbumAsync()
    {
        Album = null!;
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        Album = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(BriefAlbum);
        IsPlayAllButtonEnabled = Album.SongList.Count > 0;
        IsSearchProgressRingActive = false;
    }

    [RelayCommand]

    public void PlayAllButton()

    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        App.GetService<MusicPlayer>().PlaySongByInfo(Album.SongList[0]);
    }

    [RelayCommand]

    public void ShuffledPlayAllButton()

    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledOnlineSongs:Album:{Album.Name}",
            Album.SongList
        );
        App.GetService<MusicPlayer>().PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }



    [RelayCommand]

    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)

    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, Album.SongList);
    }

    [RelayCommand]

    public void AddToPlayQueueFlyoutButton()

    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Album:{Album.Name}",
                Album.SongList
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(Album.SongList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(Album.SongList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
    }

    [RelayCommand]

    public void PlayButton(IBriefOnlineSongInfo info)

    {
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void PlayNextButton(IBriefOnlineSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]

    public void AddToPlayQueueButton(IBriefOnlineSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]

    public async Task AddToPlaylistButton(Tuple<IBriefOnlineSongInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]

    public async Task ShowArtistButton(IBriefOnlineSongInfo info)

    {
        var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(info);
        if (onlineArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineArtistDetailPage),
                new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(OnlineAlbumDetailPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }







}

