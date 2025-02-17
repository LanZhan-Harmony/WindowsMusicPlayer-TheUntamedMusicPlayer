using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public class ArtistDetailViewModel
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();

    public ArtistInfo Artist { get; set; } = Data.SelectedArtist!;

    public List<BriefAlbumInfo> AlbumList
    {
        get; set;
    }

    public ArtistDetailViewModel()
    {
        AlbumList = Data.MusicLibrary.GetAlbumsByArtist(Artist);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList(), 0, 0);
        Data.MusicPlayer.PlaySongByInfo(AlbumList[0].SongList[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList(), 0, 0);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void SongListViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList(), 0, 0);
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void AlbumGridViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is BriefAlbumInfo briefAlbumInfo)
        {
            var songList = new ObservableCollection<BriefMusicInfo>(briefAlbumInfo.SongList);
            Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{briefAlbumInfo.Name}", songList, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
    }

    private ObservableCollection<BriefMusicInfo> ConvertAllSongsToFlatList()
    {
        return [.. AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>("ArtistDetailSelectionBarSelectedIndex");
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync("ArtistDetailSelectionBarSelectedIndex", selectedIndex);
    }
}
