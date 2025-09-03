using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;
using The_Untamed_Music_Player.Services;
using ZLogger;

namespace The_Untamed_Music_Player.Models;

public partial class OnlineMusicLibrary : ObservableObject
{
    private static readonly ILogger _logger = LoggingService.CreateLogger<OnlineMusicLibrary>();
    private bool _isSearchingMore = false;

    // 缓存上次搜索的参数，用于避免重复搜索
    private string? _lastSearchKeyWords = null;
    private byte? _lastMusicLibraryIndex = null;

    /// <summary>
    /// 页面索引, 0为歌曲, 1为专辑, 2为艺术家, 3为歌单
    /// </summary>
    public byte PageIndex { get; set; }

    /// <summary>
    /// 乐库索引, 0为网易云音乐
    /// </summary>
    public byte MusicLibraryIndex { get; set; }

    /// <summary>
    /// 搜索建议关键词
    /// </summary>
    public string SuggestKeyWords { get; set; } = null!;

    /// <summary>
    /// 实际搜索关键词
    /// </summary>
    [ObservableProperty]
    public partial string SearchKeyWords { get; set; } = null!;

    /// <summary>
    /// 显示"...的搜索结果"的文本块可见性
    /// </summary>
    [ObservableProperty]
    public partial Visibility KeyWordsTextBlockVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 网络错误可见性
    /// </summary>
    [ObservableProperty]
    public partial Visibility NetworkErrorVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 结果列表透明度(可见性)
    /// </summary>
    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    /// <summary>
    /// 加载进度环可见性
    /// </summary>
    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = false;

    /// <summary>
    /// 加载更多进度环可见性
    /// </summary>
    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; } = false;

    [ObservableProperty]
    public partial IOnlineSongInfoList OnlineSongInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial IOnlineAlbumInfoList OnlineAlbumInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial IOnlineArtistInfoList OnlineArtistInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial IOnlinePlaylistInfoList OnlinePlaylistInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial List<SuggestResult> SuggestResultList { get; set; } = [];

    public async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyWords))
        {
            KeyWordsTextBlockVisibility = Visibility.Collapsed;
            NetworkErrorVisibility = Visibility.Collapsed;
            ListViewOpacity = 0;
            return;
        }

        if (!await NetworkHelper.IsInternetAvailableAsync())
        {
            KeyWordsTextBlockVisibility = Visibility.Collapsed;
            NetworkErrorVisibility = Visibility.Visible;
            ListViewOpacity = 0;
            return;
        }

        // 检查是否需要重新搜索
        if (ShouldSkipSearch())
        {
            // 直接显示现有结果
            KeyWordsTextBlockVisibility = Visibility.Visible;
            NetworkErrorVisibility = Visibility.Collapsed;
            ListViewOpacity = 1;
            return;
        }

        KeyWordsTextBlockVisibility = Visibility.Collapsed;
        NetworkErrorVisibility = Visibility.Collapsed;
        ListViewOpacity = 0;
        IsSearchProgressRingActive = true;
        try
        {
            if (PageIndex == 0)
            {
                switch (MusicLibraryIndex)
                {
                    case 0:
                        var cloudList = OnlineSongInfoList as CloudOnlineSongInfoList ?? [];
                        OnlineSongInfoList = cloudList;
                        await CloudSongSearchHelper.SearchSongsAsync(SearchKeyWords, cloudList);
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        // TODO: 其它 MusicLibraryIndex 分支实现
                        break;
                    default:
                        // 默认行为或其它 MusicLibraryIndex 分支实现
                        break;
                }
            }
            else if (PageIndex == 1)
            {
                switch (MusicLibraryIndex)
                {
                    case 0:
                        var cloudList = OnlineAlbumInfoList as CloudOnlineAlbumInfoList ?? [];
                        OnlineAlbumInfoList = cloudList;
                        await CloudAlbumSearchHelper.SearchAlbumsAsync(SearchKeyWords, cloudList);
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        break;
                    default:
                        break;
                }
            }
            else if (PageIndex == 2)
            {
                switch (MusicLibraryIndex)
                {
                    case 0:
                        var cloudList = OnlineArtistInfoList as CloudOnlineArtistInfoList ?? [];
                        OnlineArtistInfoList = cloudList;
                        await CloudArtistSearchHelper.SearchArtistsAsync(SearchKeyWords, cloudList);
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (MusicLibraryIndex)
                {
                    case 0:
                        var cloudList = OnlinePlaylistInfoList as CloudOnlinePlaylistInfoList ?? [];
                        OnlinePlaylistInfoList = cloudList;
                        await CloudPlaylistSearchHelper.SearchPlaylistsAsync(
                            SearchKeyWords,
                            cloudList
                        );
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        break;
                    default:
                        break;
                }
            }

            // 更新缓存的搜索参数
            _lastSearchKeyWords = SearchKeyWords;
            _lastMusicLibraryIndex = MusicLibraryIndex;

            KeyWordsTextBlockVisibility = Visibility.Visible;
            ListViewOpacity = 1;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"在线搜索{SearchKeyWords}时发生错误");
        }
        finally
        {
            IsSearchProgressRingActive = false;
        }
    }

    /// <summary>
    /// 检查是否应该跳过搜索（如果搜索参数相同且已有缓存结果）
    /// </summary>
    /// <returns>如果应该跳过搜索则返回true</returns>
    private bool ShouldSkipSearch()
    {
        // 如果搜索参数没有变化
        if (_lastSearchKeyWords == SearchKeyWords && _lastMusicLibraryIndex == MusicLibraryIndex)
        {
            return PageIndex switch
            {
                0 => OnlineSongInfoList?.Count > 0 && OnlineSongInfoList.KeyWords == SearchKeyWords,
                1 => OnlineAlbumInfoList?.Count > 0
                    && OnlineAlbumInfoList.KeyWords == SearchKeyWords,
                2 => OnlineArtistInfoList?.Count > 0
                    && OnlineArtistInfoList.KeyWords == SearchKeyWords,
                3 => OnlinePlaylistInfoList?.Count > 0
                    && OnlinePlaylistInfoList.KeyWords == SearchKeyWords,
                _ => false,
            };
        }
        return false;
    }

    /// <summary>
    /// 强制重新搜索，忽略缓存
    /// </summary>
    public async Task ForceSearch()
    {
        _lastSearchKeyWords = null;
        _lastMusicLibraryIndex = null;
        await Search();
    }

    public async Task SearchMore()
    {
        if (!_isSearchingMore && !OnlineSongInfoList.HasAllLoaded)
        {
            _isSearchingMore = true;
            if (OnlineSongInfoList.HasAllLoaded || !await NetworkHelper.IsInternetAvailableAsync())
            {
                return;
            }
            IsSearchMoreProgressRingActive = true;
            try
            {
                if (PageIndex == 0)
                {
                    switch (MusicLibraryIndex)
                    {
                        case 0:
                            var cloudList = OnlineSongInfoList as CloudOnlineSongInfoList ?? [];
                            OnlineSongInfoList = cloudList;
                            await CloudSongSearchHelper.SearchMoreSongsAsync(cloudList);
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            // TODO: 其它 MusicLibraryIndex 分支实现
                            break;
                        default:
                            // 默认行为或其它 MusicLibraryIndex 分支实现
                            break;
                    }
                }
                else if (PageIndex == 1)
                {
                    switch (MusicLibraryIndex)
                    {
                        case 0:
                            var cloudList = OnlineAlbumInfoList as CloudOnlineAlbumInfoList ?? [];
                            OnlineAlbumInfoList = cloudList;
                            await CloudAlbumSearchHelper.SearchMoreAlbumsAsync(cloudList);
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            break;
                        default:
                            break;
                    }
                }
                else if (PageIndex == 2)
                {
                    switch (MusicLibraryIndex)
                    {
                        case 0:
                            var cloudList = OnlineArtistInfoList as CloudOnlineArtistInfoList ?? [];
                            OnlineArtistInfoList = cloudList;
                            await CloudArtistSearchHelper.SearchMoreArtistsAsync(cloudList);
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (MusicLibraryIndex)
                    {
                        case 0:
                            var cloudList =
                                OnlinePlaylistInfoList as CloudOnlinePlaylistInfoList ?? [];
                            OnlinePlaylistInfoList = cloudList;
                            await CloudPlaylistSearchHelper.SearchMorePlaylistsAsync(cloudList);
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogInformation(ex, $"在线搜索更多{SearchKeyWords}时发生错误");
            }
            finally
            {
                _isSearchingMore = false;
                IsSearchMoreProgressRingActive = false;
            }
        }
    }

    public async Task UpdateSuggestResult()
    {
        if (!string.IsNullOrWhiteSpace(SuggestKeyWords))
        {
            switch (MusicLibraryIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                default:
                    // TODO: 其它 MusicLibraryIndex 分支实现
                    var cloudList = OnlineSongInfoList as CloudOnlineSongInfoList ?? [];
                    OnlineSongInfoList = cloudList;
                    SuggestResultList = await CloudSuggestSearchHelper.GetSuggestAsync(
                        SuggestKeyWords
                    );
                    break;
            }
        }
        else
        {
            ClearSuggestResult();
        }
    }

    public void ClearSuggestResult()
    {
        SuggestResultList = [];
    }

    public void AutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox autoSuggestBox)
        {
            autoSuggestBox.Text = SearchKeyWords;
        }
    }

    public async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await ForceSearch();
    }
}
