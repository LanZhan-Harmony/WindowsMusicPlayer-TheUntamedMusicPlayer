using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public sealed class LyricViewModel
{
    public LyricViewModel() { }

    public void ListView_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LyricSlice lyricSlice)
        {
            var time = lyricSlice.Time;
            Data.MusicPlayer.LyricPositionUpdate(time);
        }
    }

    public void PlayButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.MusicPlayer.PlaySongByInfo(currentSong!);
    }

    public void PlayNextButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToNextPlay([currentSong!]);
    }

    public void AddToPlayQueueButton_Click(object _1, RoutedEventArgs _2)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        Data.PlayQueueManager.AddSongsToEnd([currentSong!]);
    }

    public async void AddToPlaylistButton_Click(PlaylistInfo playlist)
    {
        var currentSong = Data.PlayState.CurrentBriefSong;
        await Data.PlaylistLibrary.AddToPlaylist(playlist, currentSong!);
    }

    public async void ShowAlbumButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.PlayState.CurrentBriefSong;
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

    public async void ShowArtistButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
        var info = Data.PlayState.CurrentBriefSong;
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
