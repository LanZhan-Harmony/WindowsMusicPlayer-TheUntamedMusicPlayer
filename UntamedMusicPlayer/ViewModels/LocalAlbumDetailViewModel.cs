using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalAlbumDetailViewModel
{
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    public LocalAlbumInfo Album { get; set; } = null!;

    public List<IBriefSongInfoBase> SongList { get; set; }

    public LocalAlbumDetailViewModel()
    {
        SongList = [];
    }

    public void Initialize(LocalAlbumInfo album)
    {
        Album = album;
        SongList = [.. App.GetService<MusicLibrary>().GetSongsByAlbum(Album)];
    }

    [RelayCommand]

    public void PlayAllButton()

    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        App.GetService<MusicPlayer>().PlaySongByInfo(SongList[0]);
    }

    [RelayCommand]

    public void ShuffledPlayAllButton()

    {
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Album:{Album.Name}",
            SongList
        );
        App.GetService<MusicPlayer>().PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }



    [RelayCommand]

    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)

    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, SongList);
    }

    [RelayCommand]

    public void AddToPlayQueueFlyoutButton()

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
            App.GetService<MusicPlayer>().PlaySongByInfo(SongList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(SongList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
    }

    [RelayCommand]

    public void PlayButton(BriefLocalSongInfo info)

    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void PlayNextButton(BriefLocalSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]

    public void AddToPlayQueueButton(BriefLocalSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]

    public async Task AddToPlaylistButton(Tuple<BriefLocalSongInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]

    public void ShowArtistButton(BriefLocalSongInfo info)

    {
        var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(info.Artists[0]);
        if (localArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(LocalArtistDetailPage),
                new LocalArtistNavigationArgs(localArtistInfo, nameof(LocalAlbumDetailPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }







}

