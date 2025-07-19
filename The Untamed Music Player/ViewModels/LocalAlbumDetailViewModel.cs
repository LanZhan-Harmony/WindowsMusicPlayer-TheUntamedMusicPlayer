using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class LocalAlbumDetailViewModel : ObservableRecipient
{
    public LocalAlbumInfo Album { get; set; } = Data.SelectedLocalAlbum!;

    public List<IBriefSongInfoBase> SongList { get; set; }

    public LocalAlbumDetailViewModel()
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
        if (e.ClickedItem is IBriefSongInfoBase info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.SetPlayList($"LocalSongs:Album:{Album.Name}", SongList, 0, 0);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefSongInfoBase> { info };
            Data.MusicPlayer.SetPlayList($"LocalSongs:Part", list, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void ShowArtistButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo songInfo)
        {
            var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(songInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                Data.SelectedLocalArtist = localArtistInfo;
                Data.ShellPage!.GetFrame()
                    .Navigate(
                        typeof(LocalArtistDetailPage),
                        null,
                        new SuppressNavigationTransitionInfo()
                    );
            }
        }
    }
}
