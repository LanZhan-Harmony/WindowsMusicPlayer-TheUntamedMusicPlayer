using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public partial class AlbumDetailViewModel : ObservableRecipient
{
    public AlbumInfo Album { get; set; } = Data.SelectedAlbum!;

    public ObservableCollection<BriefMusicInfo> SongList
    {
        get; set;
    }

    public AlbumDetailViewModel()
    {
        var tempList = Data.MusicLibrary.GetSongsByAlbum(Album);
        SongList = [.. tempList];
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByPath(SongList[0].Path);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList);
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }
}
