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
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList, 0, 0);
        Data.MusicPlayer.PlaySongByInfo(SongList[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList, 0, 0);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList, 0, 0);
        if (sender is FrameworkElement { DataContext: BriefMusicInfo briefMusicInfo })
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }
}
