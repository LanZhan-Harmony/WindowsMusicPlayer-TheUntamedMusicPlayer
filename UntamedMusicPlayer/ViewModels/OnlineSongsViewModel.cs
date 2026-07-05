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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
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
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"OnlineSongs:{App.GetService<OnlineMusicLibrary>().SearchKeyWords}",
            App.GetService<OnlineMusicLibrary>().OnlineSongInfoList
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void OnlineSongsPlayNextButton(IBriefOnlineSongInfo info)

    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
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
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue("OnlineSongs:Part", list);
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

