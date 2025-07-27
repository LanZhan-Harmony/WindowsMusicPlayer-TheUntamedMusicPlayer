using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using TagLib;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using Windows.Storage;

namespace The_Untamed_Music_Player.ViewModels;

public class OnlineSongsViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    public OnlineSongsViewModel() { }

    public void OnlineSongsSongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Part:{Data.OnlineMusicLibrary.SearchKeyWords}",
            Data.OnlineMusicLibrary.OnlineSongInfoList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void OnlineSongsPlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Part:{Data.OnlineMusicLibrary.SearchKeyWords}",
            Data.OnlineMusicLibrary.OnlineSongInfoList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void OnlineSongsPlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayList("LocalSongs:Part", list, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
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
