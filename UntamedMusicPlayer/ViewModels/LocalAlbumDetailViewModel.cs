using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.ViewModels;

public class LocalAlbumDetailViewModel
{
    public LocalAlbumInfo Album { get; set; } = Data.SelectedLocalAlbum!;

    public List<IBriefSongInfoBase> SongList { get; set; }

    public LocalAlbumDetailViewModel()
    {
        SongList = [.. Data.MusicLibrary.GetSongsByAlbum(Album)];
    }

    public void PlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByInfo(SongList[0]);
    }

    public void ShuffledPlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledLocalSongs:Album:{Album.Name}",
            SongList
        );
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, SongList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
            Data.MusicPlayer.PlaySongByInfo(SongList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(SongList);
        }
    }

    public void SongListView_ItemClick(object _, ItemClickEventArgs e)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(BriefLocalSongInfo info)
    {
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(BriefLocalSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    public void AddToPlayQueueButton_Click(BriefLocalSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    public async void AddToPlaylistButton_Click(BriefLocalSongInfo info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public void ShowArtistButton_Click(BriefLocalSongInfo info)
    {
        var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(info.Artists[0]);
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
}
