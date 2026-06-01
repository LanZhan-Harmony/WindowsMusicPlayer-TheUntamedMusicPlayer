using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineSongsViewModel
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    public OnlineSongsViewModel() { }

    public void OnlineSongsSongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.PlayQueueManager.SetNormalPlayQueue(
            $"OnlineSongs:{App.GetService<OnlineMusicLibrary>().SearchKeyWords}",
            App.GetService<OnlineMusicLibrary>().OnlineSongInfoList
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
    }

    [RelayCommand]

    public void OnlineSongsPlayButton(IBriefOnlineSongInfo info)

    {
        Data.PlayQueueManager.SetNormalPlayQueue(
            $"OnlineSongs:{App.GetService<OnlineMusicLibrary>().SearchKeyWords}",
            App.GetService<OnlineMusicLibrary>().OnlineSongInfoList
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void OnlineSongsPlayNextButton(IBriefOnlineSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
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
            Data.PlayQueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
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

    public async Task ShowAlbumButton(IBriefOnlineSongInfo info)

    {
        var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(info);
        if (onlineAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineAlbumDetailPage),
                new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(OnlineSongsPage)),
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
                new OnlineArtistNavigationArgs(onlineArtistInfo, nameof(OnlineSongsPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    // Legacy click-handler adapters kept for batch-refactor compatibility.





}

