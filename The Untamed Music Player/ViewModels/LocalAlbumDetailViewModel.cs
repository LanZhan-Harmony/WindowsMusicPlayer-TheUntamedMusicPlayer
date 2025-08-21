using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public class LocalAlbumDetailViewModel
{
    public LocalAlbumInfo Album { get; set; } = Data.SelectedLocalAlbum!;

    public List<IBriefSongInfoBase> SongList { get; set; }

    public LocalAlbumDetailViewModel()
    {
        SongList = [.. Data.MusicLibrary.GetSongsByAlbum(Album)];
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByInfo(SongList[0]);
    }

    public void ShuffledPlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetShuffledPlayQueue($"ShuffledLocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.MusicPlayer.ShuffledPlayQueue[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, SongList);
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
            Data.MusicPlayer.PlaySongByInfo(SongList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToPlayQueue(SongList);
        }
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(BriefLocalSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}", SongList);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void AddToPlayQueueButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Album:{Album.Name}:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToPlayQueue(info);
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
