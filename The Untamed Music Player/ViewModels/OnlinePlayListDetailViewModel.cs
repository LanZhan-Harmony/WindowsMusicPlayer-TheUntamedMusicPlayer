using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class OnlinePlayListDetailViewModel : ObservableObject
{
    private IBriefOnlinePlaylistInfo? _cachedBriefPlaylist = null;
    public IBriefOnlinePlaylistInfo BriefPlaylist { get; set; } = Data.SelectedOnlinePlaylist!;

    [ObservableProperty]
    public partial IDetailedOnlinePlaylistInfo Playlist { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    public OnlinePlayListDetailViewModel() { }

    public async void CheckAndLoadPlaylistAsync()
    {
        BriefPlaylist = Data.SelectedOnlinePlaylist!;
        if (ShouldReloadPlaylist())
        {
            await LoadPlaylistAsync();
            _cachedBriefPlaylist = BriefPlaylist;
        }
        else
        {
            ListViewOpacity = 1;
            IsSearchProgressRingActive = false;
        }
    }

    private bool ShouldReloadPlaylist()
    {
        if (_cachedBriefPlaylist is null || Playlist is null)
        {
            return true;
        }
        if (_cachedBriefPlaylist.ID != BriefPlaylist.ID)
        {
            return true;
        }
        return false;
    }

    private async Task LoadPlaylistAsync()
    {
        Playlist = null!;
        ListViewOpacity = 0;
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        Playlist = await IDetailedOnlinePlaylistInfo.CreateDetailedOnlinePlaylistInfoAsync(
            BriefPlaylist
        );
        IsPlayAllButtonEnabled = Playlist.SongList.Count > 0;
        ListViewOpacity = 1;
        IsSearchProgressRingActive = false;
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (Playlist.SongList.Count == 0)
        {
            return;
        }
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Playlist:{Playlist.Name}", Playlist.SongList);
        Data.MusicPlayer.PlaySongByInfo(Playlist.SongList[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Playlist:{Playlist.Name}", Playlist.SongList);
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Playlist:{Playlist.Name}", Playlist.SongList);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Part", list);
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
