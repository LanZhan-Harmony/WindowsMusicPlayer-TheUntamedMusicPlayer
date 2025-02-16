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
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList());
        Data.MusicPlayer.PlaySongByPath(AlbumList[0].SongList[0].Path);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList());
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void SongListViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Artist:{Artist.Name}", ConvertAllSongsToFlatList());
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void AlbumGridViewPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is BriefAlbumInfo briefAlbumInfo)
        {
            var songList = new ObservableCollection<BriefMusicInfo>(briefAlbumInfo.SongList);
            Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{briefAlbumInfo.Name}", songList);
            Data.MusicPlayer.PlaySongByPath(songList[0].Path);
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
