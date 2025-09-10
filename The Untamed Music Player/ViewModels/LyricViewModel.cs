using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public class LyricViewModel
{
    public LyricViewModel() { }

    public void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LyricSlice lyricSlice)
        {
            var time = lyricSlice.Time;
            Data.MusicPlayer.LyricProgressUpdate(time);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        Data.MusicPlayer.PlaySongByInfo(currentSong!);
    }

    public void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        Data.MusicPlayer.AddSongToNextPlay(currentSong!);
    }

    public void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        Data.MusicPlayer.AddSongToPlayQueue(currentSong!);
    }

    public async void AddToPlaylistButton_Click(PlaylistInfo playlist)
    {
        var currentSong = Data.MusicPlayer.CurrentBriefSong;
        await Data.PlaylistLibrary.AddToPlaylist(playlist, currentSong!);
    }

    public async void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.MusicPlayer.CurrentBriefSong;
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
            var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(onlineInfo);
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

    public async void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.MusicPlayer.CurrentBriefSong;
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
            var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(onlineInfo);
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
}
