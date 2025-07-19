using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayQueueViewModel : ObservableRecipient
{
    public PlayQueueViewModel() { }

    public void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefSongInfoBase info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.AddSongToNextPlay(info);
    }

    public async void RemoveButton_Click(IBriefSongInfoBase info)
    {
        await Data.MusicPlayer.RemoveSong(info);
    }

    public void MoveUpButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.MoveUpSong(info);
    }

    public void MoveDownButton_Click(IBriefSongInfoBase info)
    {
        Data.MusicPlayer.MoveDownSong(info);
    }

    public void ShowAlbumButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.SourceMode == 0)
        {
            var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(
                ((BriefLocalSongInfo)info).Album
            );
            if (localAlbumInfo is not null)
            {
                Data.SelectedAlbum = localAlbumInfo;
                Data.ShellPage!.GetFrame()
                    .Navigate(
                        typeof(LocalAlbumDetailPage),
                        null,
                        new SuppressNavigationTransitionInfo()
                    );
            }
        }
    }

    public void ShowArtistButton_Click(IBriefSongInfoBase info)
    {
        if (Data.MusicPlayer.SourceMode == 0)
        {
            var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(
                ((BriefLocalSongInfo)info).Artists[0]
            );
            if (localArtistInfo is not null)
            {
                Data.SelectedArtist = localArtistInfo;
                Data.ShellPage!.GetFrame()
                    .Navigate(
                        typeof(LocalArtistDetailPage),
                        null,
                        new SuppressNavigationTransitionInfo()
                    );
            }
        }
    }

    public void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.ClearPlayQueue();
    }
}
