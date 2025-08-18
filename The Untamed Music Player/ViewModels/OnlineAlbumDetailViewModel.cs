using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class OnlineAlbumDetailViewModel : ObservableObject
{
    private IBriefOnlineAlbumInfo? _cachedBriefAlbum = null;
    public IBriefOnlineAlbumInfo BriefAlbum { get; set; } = Data.SelectedOnlineAlbum!;

    [ObservableProperty]
    public partial IDetailedOnlineAlbumInfo Album { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPlayAllButtonEnabled { get; set; } = false;

    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    public OnlineAlbumDetailViewModel() { }

    public async void CheckAndLoadAlbumAsync()
    {
        BriefAlbum = Data.SelectedOnlineAlbum!;
        if (ShouldReloadAlbum())
        {
            await LoadAlbumAsync();
            _cachedBriefAlbum = BriefAlbum;
        }
        else
        {
            ListViewOpacity = 1;
            IsSearchProgressRingActive = false;
        }
    }

    private bool ShouldReloadAlbum()
    {
        if (_cachedBriefAlbum is null || Album is null)
        {
            return true;
        }
        if (_cachedBriefAlbum.ID != BriefAlbum.ID)
        {
            return true;
        }
        return false;
    }

    private async Task LoadAlbumAsync()
    {
        Album = null!;
        ListViewOpacity = 0;
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        Album = await IDetailedOnlineAlbumInfo.CreateDetailedOnlineAlbumInfoAsync(BriefAlbum);
        IsPlayAllButtonEnabled = Album.SongList.Count > 0;
        ListViewOpacity = 1;
        IsSearchProgressRingActive = false;
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.MusicPlayer.SetPlayQueue(
            $"OnlineSongs:Album:{Album.Name}",
            Album.SongList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(Album.SongList[0]);
    }

    public void ShuffledPlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.MusicPlayer.SetShuffledPlayQueue(
            $"ShuffledOnlineSongs:Album:{Album.Name}",
            Album.SongList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(Data.MusicPlayer.ShuffledPlayQueue[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue(
            $"OnlineSongs:Album:{Album.Name}",
            Album.SongList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList, 0, 0);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"OnlineSongs:Part", list, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
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
