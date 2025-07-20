using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public partial class OnlineAlbumDetailViewModel : ObservableRecipient
{
    public IBriefOnlineAlbumInfo BriefAlbum { get; set; } = Data.SelectedOnlineAlbum!;

    [ObservableProperty]
    public partial IDetailedOnlineAlbumInfo Album { get; set; } =
        IDetailedOnlineAlbumInfo.CreateFastOnlineAlbumInfoAsync(Data.SelectedOnlineAlbum!);

    public OnlineAlbumDetailViewModel()
    {
        LoadAlbumAsync();
    }

    private async void LoadAlbumAsync()
    {
        Album = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(BriefAlbum);
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Album:{Album.Name}",
            Album.SongList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(Album.SongList[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Album:{Album.Name}",
            Album.SongList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        if (e.ClickedItem is IBriefSongInfoBase info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }
}
