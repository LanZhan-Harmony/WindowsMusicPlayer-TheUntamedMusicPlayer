using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalAlbumDetailViewModel
{
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;

    public LocalAlbumInfo Album { get; set; } = null!;

    public List<IBriefSongInfoBase> SongList { get; set; }

    public LocalAlbumDetailViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
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
        _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        _musicPlayer.PlaySongByInfo(SongList[0]);
    }

    [RelayCommand]
    public void ShuffledPlayAllButton()
    {
        _musicPlayer.QueueManager.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Album:{Album.Name}",
            SongList
        );
        _musicPlayer.PlaySongByIndexedInfo(_musicPlayer.QueueManager.CurrentQueue[0]);
    }

    [RelayCommand]
    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)
    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, SongList);
    }

    [RelayCommand]
    public void AddToPlayQueueFlyoutButton()
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Album:{Album.Name}",
                SongList
            );
            _musicPlayer.PlaySongByInfo(SongList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(SongList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            _musicPlayer.PlaySongByInfo(info);
        }
    }

    [RelayCommand]
    public void PlayButton(BriefLocalSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void PlayNextButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Album:{Album.Name}:Part",
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
    public void AddToPlayQueueButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Album:{Album.Name}:Part",
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
