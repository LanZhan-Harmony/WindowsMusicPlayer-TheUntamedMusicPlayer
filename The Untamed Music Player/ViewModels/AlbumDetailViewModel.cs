using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public partial class AlbumDetailViewModel : ObservableRecipient
{
    public AlbumInfo Album
    {
        get; set;
    }

    public ObservableCollection<BriefMusicInfo> SongList { get; set; } = [];

    public AlbumDetailViewModel()
    {
        Album = Data.SelectedAlbum ?? new AlbumInfo();
        SongList = Data.MusicLibrary.GetMusicByAlbum(Album);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"Songs:Album:{Album.Name}", SongList, 0);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"Songs:Album:{Album.Name}", SongList, 0);
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }
}
