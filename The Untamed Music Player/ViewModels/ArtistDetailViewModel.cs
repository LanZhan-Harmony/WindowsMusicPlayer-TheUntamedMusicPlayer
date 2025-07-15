using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public class ArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public ArtistInfo Artist { get; set; } = Data.SelectedArtist!;

    public List<BriefAlbumInfo> AlbumList { get; set; }

    public ArtistDetailViewModel()
    {
        AlbumList = Data.MusicLibrary.GetAlbumsByArtist(Artist);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            0,
            0
        );
        Data.MusicPlayer.PlaySongByInfo(AlbumList[0].SongList[0]);
    }

    public void SongListView_ItemClick(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.SetPlayList(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            0,
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.SetPlayList(
            $"LocalSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            0,
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayNextButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            Data.MusicPlayer.SetPlayList("LocalSongs:Part", list, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void SongListViewShowAlbumButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefSongInfo SongInfo)
        {
            var albumInfo = Data.MusicLibrary.GetAlbumInfoBySong(SongInfo.Album);
            if (albumInfo is not null)
            {
                Data.SelectedAlbum = albumInfo;
                Data.ShellPage!.GetFrame()
                    .Navigate(
                        typeof(AlbumDetailPage),
                        null,
                        new SuppressNavigationTransitionInfo()
                    );
            }
        }
    }

    public void AlbumGridViewPlayButton_Click(BriefAlbumInfo info)
    {
        var songList = info.SongList;
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{info.Name}", songList, 0, 0);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void AlbumGridViewPlayNextButton_Click(BriefAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{info.Name}", songList, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    private List<IBriefSongInfoBase> ConvertAllSongsToFlatList()
    {
        return [.. AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "ArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "ArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }
}
