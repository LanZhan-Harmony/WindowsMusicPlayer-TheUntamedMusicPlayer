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

    public async void ShowAlbumButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(localInfo.Album);
            if (localAlbumInfo is not null)
            {
                Data.SelectedLocalAlbum = localAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfo(onlineInfo);
            if (onlineAlbumInfo is not null)
            {
                Data.SelectedOnlineAlbum = onlineAlbumInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineAlbumDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
    }

    public async void ShowArtistButton_Click(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo localInfo)
        {
            var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(localInfo.Artists[0]);
            if (localArtistInfo is not null)
            {
                Data.SelectedLocalArtist = localArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(LocalArtistDetailPage),
                    "",
                    new SuppressNavigationTransitionInfo()
                );
            }
        }
        else if (info is IBriefOnlineSongInfo onlineInfo)
        {
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfo(onlineInfo);
            if (onlineArtistInfo is not null)
            {
                Data.SelectedOnlineArtist = onlineArtistInfo;
                Data.ShellPage!.Navigate(
                    nameof(OnlineArtistDetailPage),
                    "",
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
