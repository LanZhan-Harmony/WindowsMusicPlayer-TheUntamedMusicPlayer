using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    public LocalArtistInfo Artist { get; set; } = null!;

    public List<LocalArtistAlbumInfo> AlbumList { get; set; } = [];

    public LocalArtistDetailViewModel() { }

    public void Initialize(LocalArtistInfo artist)
    {
        Artist = artist;
        AlbumList = App.GetService<MusicLibrary>().GetAlbumsByArtist(Artist);
    }

    [RelayCommand]

    public void PlayAllButton()

    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(AlbumList[0].SongList[0]);
    }

    [RelayCommand]

    public void ShuffledPlayAllButton()

    {
        App.GetService<MusicPlayer>().QueueManager.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByIndexedInfo(App.GetService<MusicPlayer>().QueueManager.CurrentQueue[0]);
    }



    [RelayCommand]

    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)

    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, ConvertAllSongsToFlatArray());
    }

    [RelayCommand]

    public void AddToPlayQueueFlyoutButton()

    {
        var allSongs = ConvertAllSongsToFlatArray();
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{Artist.Name}", allSongs);
            App.GetService<MusicPlayer>().PlaySongByInfo(allSongs[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(allSongs);
        }
    }

    public void SongListView_ItemClick(BriefLocalSongInfo info)
    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void SongListViewPlayButton(BriefLocalSongInfo info)

    {
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void SongListViewPlayNextButton(BriefLocalSongInfo info)

    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{Artist.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]

    public void SongListViewAddToPlayQueueButton(BriefLocalSongInfo info)

    {
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{Artist.Name}:Part", list);
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]

    public async Task SongListViewAddToPlaylistButton(Tuple<BriefLocalSongInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]

    public void SongListViewShowAlbumButton(BriefLocalSongInfo info)

    {
        var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(info.Album);
        if (localAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(LocalAlbumDetailPage),
                new LocalAlbumNavigationArgs(localAlbumInfo, nameof(LocalArtistDetailPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    [RelayCommand]

    public void AlbumGridViewPlayButton(LocalArtistAlbumInfo info)

    {
        var songList = info.SongList;
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public void AlbumGridViewPlayNextButton(LocalArtistAlbumInfo info)

    {
        var songList = info.SongList;
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]

    public void AlbumGridViewAddToPlayQueueButton(LocalArtistAlbumInfo info)

    {
        var songList = info.SongList;
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]

    public async Task AlbumGridViewAddToPlaylistButton(Tuple<LocalArtistAlbumInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info.SongList);
    }

    private IBriefSongInfoBase[] ConvertAllSongsToFlatArray()
    {
        return [.. AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "LocalArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async Task SaveSelectionBarSelectedIndexAsync(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "LocalArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }











}

