using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public class LocalArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public LocalArtistInfo Artist { get; set; } = Data.SelectedLocalArtist!;

    public List<LocalArtistAlbumInfo> AlbumList { get; set; }

    public LocalArtistDetailViewModel()
    {
        AlbumList = Data.MusicLibrary.GetAlbumsByArtist(Artist);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByInfo(AlbumList[0].SongList[0]);
    }

    public void ShuffledPlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.MusicPlayer.ShuffledPlayQueue[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, ConvertAllSongsToFlatList());
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        var allSongs = ConvertAllSongsToFlatList();
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Artist:{Artist.Name}", allSongs);
            Data.MusicPlayer.PlaySongByInfo(allSongs[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(allSongs);
        }
    }

    public void SongListView_ItemClick(BriefLocalSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayButton_Click(BriefLocalSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayNextButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Artist:{Artist.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void SongListViewAddToPlayQueueButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Artist:{Artist.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToPlayQueue(info);
        }
    }

    public async void SongListViewAddToPlaylistButton_Click(
        BriefLocalSongInfo info,
        PlaylistInfo playlist
    )
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public void SongListViewShowAlbumButton_Click(BriefLocalSongInfo info)
    {
        var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(info.Album);
        if (localAlbumInfo is not null)
        {
            Data.SelectedLocalAlbum = localAlbumInfo;
            Data.ShellPage!.Navigate(
                nameof(LocalAlbumDetailPage),
                "",
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    public void AlbumGridViewPlayButton_Click(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void AlbumGridViewPlayNextButton_Click(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    public void AlbumGridViewAddToPlayQueueButton_Click(LocalArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(songList);
        }
    }

    public async void AlbumGridViewAddToPlaylistButton_Click(
        LocalArtistAlbumInfo info,
        PlaylistInfo playlist
    )
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info.SongList);
    }

    private List<IBriefSongInfoBase> ConvertAllSongsToFlatList()
    {
        return [.. AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "LocalArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "LocalArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }
}
