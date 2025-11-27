using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

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

    public void PlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        Data.MusicPlayer.PlaySongByInfo(Album.SongList[0]);
    }

    public void ShuffledPlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledOnlineSongs:Album:{Album.Name}",
            Album.SongList
        );
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, Album.SongList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        if (Album.SongList.Count == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Album:{Album.Name}",
                Album.SongList
            );
            Data.MusicPlayer.PlaySongByInfo(Album.SongList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(Album.SongList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}", Album.SongList);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    public void AddToPlayQueueButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    public async void AddToPlaylistButton_Click(IBriefOnlineSongInfo info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
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
