using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public class OnlineSongsViewModel
{
    public OnlineSongsViewModel() { }

    public void OnlineSongsSongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"OnlineSongs:{Data.OnlineMusicLibrary.SearchKeyWords}",
            Data.OnlineMusicLibrary.OnlineSongInfoList
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void OnlineSongsPlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"OnlineSongs:{Data.OnlineMusicLibrary.SearchKeyWords}",
            Data.OnlineMusicLibrary.OnlineSongInfoList
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void OnlineSongsPlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue("OnlineSongs:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void AddToPlayQueueButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue("OnlineSongs:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToPlayQueue(info);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefOnlineSongInfo info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public async void ShowAlbumButton_Click(IBriefOnlineSongInfo info)
    {
        var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(info);
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

    public async void ShowArtistButton_Click(IBriefOnlineSongInfo info)
    {
        var onlineArtistInfo = await IBriefOnlineArtistInfo.CreateFromSongInfoAsync(info);
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
