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
        if (e.ClickedItem is IBriefMusicInfoBase info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefMusicInfoBase info)
    {
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefMusicInfoBase info)
    {
        Data.MusicPlayer.AddSongToNextPlay(info);
    }

    public async void RemoveButton_Click(IBriefMusicInfoBase info)
    {
        await Data.MusicPlayer.RemoveSong(info);
    }

    public void MoveUpButton_Click(IBriefMusicInfoBase info)
    {
        Data.MusicPlayer.MoveUpSong(info);
    }

    public void MoveDownButton_Click(IBriefMusicInfoBase info)
    {
        Data.MusicPlayer.MoveDownSong(info);
    }

    public void ShowAlbumButton_Click(IBriefMusicInfoBase info)
    {
        if (Data.MusicPlayer.SourceMode == 0)
        {
            var albumInfo = Data.MusicLibrary.GetAlbumInfoBySong(((BriefMusicInfo)info).Album);
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

    public void ShowArtistButton_Click(IBriefMusicInfoBase info)
    {
        if (Data.MusicPlayer.SourceMode == 0)
        {
            var artistInfo = Data.MusicLibrary.GetArtistInfoBySong(
                ((BriefMusicInfo)info).Artists[0]
            );
            if (artistInfo is not null)
            {
                Data.SelectedArtist = artistInfo;
                Data.ShellPage!.GetFrame()
                    .Navigate(
                        typeof(ArtistDetailPage),
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
