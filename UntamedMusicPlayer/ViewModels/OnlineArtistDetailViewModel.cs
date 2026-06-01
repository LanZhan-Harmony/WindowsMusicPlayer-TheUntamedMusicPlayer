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

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class OnlineArtistDetailViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();
    private readonly ILogger _logger = LoggingService.CreateLogger<OnlinePlayListDetailViewModel>();

    private bool _isSearchingMore = false;
    private IBriefOnlineArtistInfo? _cachedBriefArtist = null;

    public IBriefOnlineArtistInfo BriefArtist { get; set; } = null!;

    [ObservableProperty]
    public partial IDetailedOnlineArtistInfo Artist { get; set; } = null!;

    public bool IsPlayAllButtonEnabled => Artist is not null && Artist.AlbumList.Count > 0;

    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; } = false;

    public OnlineArtistDetailViewModel() { }

    /// <summary>
    /// 检查并加载艺术家数据，只在艺术家变化时重新搜索
    /// </summary>
    public async Task Initialize(IBriefOnlineArtistInfo briefArtist)
    {
        BriefArtist = briefArtist;

        // 检查是否需要重新加载
        if (ShouldReloadArtist())
        {
            await LoadArtistAsync();
            _cachedBriefArtist = BriefArtist;
        }
        else
        {
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
        IsSearchProgressRingActive = true;

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            IsSearchProgressRingActive = false;
            return;
        }

        try
        {
            Artist = await IDetailedOnlineArtistInfo.SearchArtistDetailAsync(BriefArtist);
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

    [RelayCommand]

    public void PlayAllButton()

    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        var allSongs = ConvertAllSongsToFlatArray();
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{Artist.Name}", allSongs);
        App.GetService<MusicPlayer>().PlaySongByInfo(allSongs[0]);
    }

    [RelayCommand]

    public void ShuffledPlayAllButton()

    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        Data.PlayQueueManager.SetShuffledPlayQueue(
            $"ShuffledOnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByIndexedInfo(Data.PlayQueueManager.CurrentQueue[0]);
    }



    [RelayCommand]

    public async Task AddToPlaylistFlyoutButton(PlaylistInfo playlist)

    {
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, ConvertAllSongsToFlatArray());
    }

    [RelayCommand]

    public void AddToPlayQueueFlyoutButton()

    {
        if (Artist.AlbumList.Count == 0)
        {
            return;
        }
        var allSongs = ConvertAllSongsToFlatArray();
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Artist:{Artist.Name}", allSongs);
            App.GetService<MusicPlayer>().PlaySongByInfo(allSongs[0]);
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
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void SongListViewPlayButton(IBriefOnlineSongInfo info)

    {
        Data.PlayQueueManager.SetNormalPlayQueue(
            $"OnlineSongs:Artist:{Artist.Name}",
            ConvertAllSongsToFlatArray()
        );
        App.GetService<MusicPlayer>().PlaySongByInfo(info);
    }

    [RelayCommand]

    public void SongListViewPlayNextButton(IBriefOnlineSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{Artist.Name}:Part",
                list
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay([info]);
        }
    }

    [RelayCommand]

    public void SongListViewAddToPlayQueueButton(IBriefOnlineSongInfo info)

    {
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.PlayQueueManager.SetNormalPlayQueue(
                $"OnlineSongs:Artist:{Artist.Name}:Part",
                list
            );
            App.GetService<MusicPlayer>().PlaySongByInfo(info);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]

    public async Task SongListViewAddToPlaylistButton(Tuple<IBriefOnlineSongInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]

    public async Task SongListViewShowAlbumButton(IBriefOnlineSongInfo info)

    {
        var onlineAlbumInfo = await IBriefOnlineAlbumInfo.CreateFromSongInfoAsync(info);
        if (onlineAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(OnlineAlbumDetailPage),
                new OnlineAlbumNavigationArgs(onlineAlbumInfo, nameof(OnlineArtistDetailPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    [RelayCommand]

    public void AlbumGridViewPlayButton(IOnlineArtistAlbumInfo info)

    {
        var songList = info.SongList;
        Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public void AlbumGridViewPlayNextButton(IOnlineArtistAlbumInfo info)

    {
        var songList = info.SongList;
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]

    public void AlbumGridViewAddToPlayQueueButton(IOnlineArtistAlbumInfo info)

    {
        var songList = info.SongList;
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"OnlineSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]

    public async Task AlbumGridViewAddToPlaylistButton(Tuple<IOnlineArtistAlbumInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info.SongList);
    }

    private IBriefSongInfoBase[] ConvertAllSongsToFlatArray()
    {
        return [.. Artist.AlbumList.SelectMany(album => album.SongList)];
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "LocalArtistDetailSelectionBarSelectedIndex"
        );
    }

    public async Task SaveSelectionBarSelectedIndexAsync(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "LocalArtistDetailSelectionBarSelectedIndex",
            selectedIndex
        );
    }











}

