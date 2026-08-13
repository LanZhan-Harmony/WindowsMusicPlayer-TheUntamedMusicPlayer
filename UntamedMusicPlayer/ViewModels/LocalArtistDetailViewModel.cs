using CommunityToolkit.Mvvm.Input;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly MusicPlayer _musicPlayer;

    public LocalArtistInfo Artist { get; set; } = null!;

    public List<LocalArtistAlbumInfo> AlbumList { get; set; } = [];

    public LocalArtistDetailViewModel(MusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer;
    }

    public void Initialize(LocalArtistInfo artist)
    {
        Artist = artist;
        AlbumList = App.GetService<MusicLibrary>().GetAlbumsByArtist(Artist);
    }

    [RelayCommand]
    public void PlayAllButton()
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        _musicPlayer.PlaySongByInfo(AlbumList[0].SongList[0]);
    }

    [RelayCommand]
    public void ShuffledPlayAllButton()
    {
        _musicPlayer.QueueManager.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        _musicPlayer.PlaySongByIndexedInfo(_musicPlayer.QueueManager.CurrentQueue[0]);
    }

    [RelayCommand]
    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)
    {
        await App.GetService<PlaylistLibrary>()
            .AddToPlaylist(playlist, ConvertAllSongsToFlatArray());
    }

    [RelayCommand]
    public void AddToPlayQueueFlyoutButton()
    {
        var allSongs = ConvertAllSongsToFlatArray();
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Artist:{Artist.Name}",
                allSongs
            );
            _musicPlayer.PlaySongByInfo(allSongs[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(allSongs);
        }
    }

    public void SongListView_ItemClick(BriefLocalSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void SongListViewPlayButton(BriefLocalSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void SongListViewPlayNextButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Artist:{Artist.Name}:Part",
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
    public void SongListViewAddToPlayQueueButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Artist:{Artist.Name}:Part",
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
                NavigationTransition.Suppress
            );
        }
    }

    [RelayCommand]
    public void AlbumGridViewPlayButton(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    public void AlbumGridViewPlayNextButton(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]
    public void AlbumGridViewAddToPlayQueueButton(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]
    public async Task AlbumGridViewAddToPlaylistButton(
        Tuple<LocalArtistAlbumInfo, PlaylistInfo> tuple
    )
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
