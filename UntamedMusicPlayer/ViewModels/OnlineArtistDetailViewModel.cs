using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.Views;
using ZLogger;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineArtistDetailViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly ILogger _logger = LoggingService.CreateLogger<OnlinePlayListDetailViewModel>();

    private bool _isSearchingMore = false;
    private IBriefOnlineArtistInfo? _cachedBriefArtist = null;

    public IBriefOnlineArtistInfo BriefArtist { get; set; } = Data.SelectedOnlineArtist!;

    [ObservableProperty]
    public partial IDetailedOnlineArtistInfo Artist { get; set; } = null!;

    public bool IsPlayAllButtonEnabled => Artist is not null && Artist.AlbumList.Count > 0;

    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; } = false;

    public OnlineArtistDetailViewModel() { }

    /// <summary>
    /// 检查并加载艺术家数据，只在艺术家变化时重新搜索
    /// </summary>
    public async void CheckAndLoadArtistAsync()
    {
        // 更新当前选中的艺术家
        BriefArtist = Data.SelectedOnlineArtist!;

        // 检查是否需要重新加载
        if (ShouldReloadArtist())
        {
            await LoadArtistAsync();
            _cachedBriefArtist = BriefArtist;
        }
        else
        {
            ListViewOpacity = 1;
            IsSearchProgressRingActive = false;
        }
    }

    /// <summary>
    /// 判断是否需要重新加载艺术家数据
    /// </summary>
    /// <returns>如果需要重新加载返回true</returns>
    private bool ShouldReloadArtist()
    {
        // 如果没有缓存的艺术家或当前艺术家为空，需要加载
        if (_cachedBriefArtist is null || Artist is null)
        {
            return true;
        }

        // 如果艺术家ID变化了，需要重新加载
        if (_cachedBriefArtist.ID != BriefArtist.ID)
        {
            return true;
        }

        return false;
    }

    private async Task LoadArtistAsync()
    {
        Artist = null!;
        ListViewOpacity = 0;
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        try
        {
            Artist = await IDetailedOnlineArtistInfo.SearchArtistDetailAsync(BriefArtist);
            ListViewOpacity = 1;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"加载艺术家{Artist.Name}详情时发生错误");
        }
        finally
        {
            IsSearchProgressRingActive = false;
            OnPropertyChanged(nameof(IsPlayAllButtonEnabled));
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
                _logger.ZLogInformation(ex, $"加载更多艺术家{Artist.Name}详情时发生错误");
            }
            finally
            {
                _isSearchingMore = false;
                IsSearchMoreProgressRingActive = false;
                OnPropertyChanged(nameof(IsPlayAllButtonEnabled));
            }
        }
    }

    public void PlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        var allSongs = ConvertAllSongsToFlatList();
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{Artist.Name}", allSongs);
        Data.MusicPlayer.PlaySongByInfo(allSongs[0]);
    }

    public void ShuffledPlayAllButton_Click(object _1, RoutedEventArgs _2)
    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledOnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }

    public async void AddToPlaylistFlyoutButton_Click(PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, ConvertAllSongsToFlatList());
    }

    public void AddToPlayQueueFlyoutButton_Click()
    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        var allSongs = ConvertAllSongsToFlatList();
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{Artist.Name}", allSongs);
            Data.MusicPlayer.PlaySongByInfo(allSongs[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(allSongs);
        }
    }

    public void SongListView_ItemClick(IBriefOnlineSongInfo info)
    {
        Data.PlayQueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.PlayQueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatList()
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void SongListViewPlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{Artist.Name}:Part",
                list
            );
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    public void SongListViewAddToPlayQueueButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{Artist.Name}:Part",
                list
            );
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    public async void SongListViewAddToPlaylistButton_Click(
        IBriefOnlineSongInfo info,
        PlaylistInfo playlist
    )
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
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
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void AlbumGridViewPlayNextButton_Click(IOnlineArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    public void AlbumGridViewAddToPlayQueueButton_Click(IOnlineArtistAlbumInfo info)
    {
        var songList = info.SongList;
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    public async void AlbumGridViewAddToPlaylistButton_Click(
        IOnlineArtistAlbumInfo info,
        PlaylistInfo playlist
    )
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info.SongList);
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
