using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.ViewModels;

public partial class OnlineArtistDetailViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    private bool _isSearchingMore = false;

    public IBriefOnlineArtistInfo BriefArtist { get; set; } = Data.SelectedOnlineArtist!;

    [ObservableProperty]
    public partial IDetailedOnlineArtistInfo Artist { get; set; } = null!;

    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; } = false;

    public OnlineArtistDetailViewModel()
    {
        LoadArtistAsync();
    }

    private async void LoadArtistAsync()
    {
        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            return;
        }
        try
        {
            Artist = await IDetailedOnlineArtistInfo.SearchArtistDetailAsync(BriefArtist);
            ListViewOpacity = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            IsSearchProgressRingActive = false;
        }
    }

    public async Task SearchMore()
    {
        if (!_isSearchingMore && !Artist.HasAllLoaded)
        {
            _isSearchingMore = true;
            if (Artist.HasAllLoaded || !await NetworkHelper.IsInternetAvailableAsync())
            {
                return;
            }
            IsSearchMoreProgressRingActive = true;
            try
            {
                await IDetailedOnlineArtistInfo.SearchMoreArtistDetailAsync(Artist);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _isSearchingMore = false;
                IsSearchMoreProgressRingActive = false;
            }
        }
    }

    public void PlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(Artist.AlbumList[0].SongList[0]);
    }

    public void SongListView_ItemClick(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList(),
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayList(
                "OnlineSongs:Part",
                list,
                (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
                0
            );
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public async void SongListViewShowAlbumButton_Click(IBriefOnlineSongInfo info)
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

    public void AlbumGridViewPlayButton_Click(IOnlineArtistAlbumInfo info)
    {
        var songList = info.SongList;
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Album:{info.Name}",
            songList,
            (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void AlbumGridViewPlayNextButton_Click(IOnlineArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayList(
                $"OnlineSongs:Album:{info.Name}",
                songList,
                (byte)(Data.OnlineMusicLibrary.MusicLibraryIndex + 1),
                0
            );
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    private List<IBriefSongInfoBase> ConvertAllSongsToFlatList()
    {
        return [.. Artist.AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "LocalArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "LocalArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }
}
