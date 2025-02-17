using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

namespace The_Untamed_Music_Player.Models;
public partial class OnlineMusicLibrary : ObservableRecipient
{
    private bool _isSearchingMore = false;

    public byte PageIndex { get; set; }
    public byte MusicLibraryIndex { get; set; }
    public string KeyWords { get; set; } = null!;

    [ObservableProperty]
    public partial string KeyWordsText { get; set; } = null!;

    [ObservableProperty]
    public partial Visibility KeyWordsTextBlockVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility NetworkErrorVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial double ListViewOpacity { get; set; } = 0;

    /// <summary>
    /// 是否显示加载进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsSearchProgressRingActive { get; set; } = false;

    /// <summary>
    /// 是否显示加载更多进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsSearchMoreProgressRingActive { get; set; } = false;

    [ObservableProperty]
    public partial IBriefOnlineMusicInfoList OnlineMusicInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial List<SearchResult> SearchResultList { get; set; } = [];

    public async Task Search()
    {
        KeyWordsTextBlockVisibility = Visibility.Collapsed;
        NetworkErrorVisibility = Visibility.Collapsed;
        ListViewOpacity = 0;
        if (!await IsInternetAvailableAsync())
        {
            NetworkErrorVisibility = Visibility.Visible;
            return;
        }
        IsSearchProgressRingActive = true;
        try
        {
            if (PageIndex == 0)
            {
                if (MusicLibraryIndex == 0)
                {
                    if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                    {
                        OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                    }
                    await OnlineMusicInfoList.SearchAsync(KeyWords);
                }
                else if (MusicLibraryIndex == 1)
                { }
                else if (MusicLibraryIndex == 2)
                { }
                else if (MusicLibraryIndex == 3)
                { }
                else if (MusicLibraryIndex == 4)
                { }
                else
                { }
            }
            KeyWordsText = KeyWords;
            KeyWordsTextBlockVisibility = Visibility.Visible;
            ListViewOpacity = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            IsSearchProgressRingActive = false;
        }
    }

    public async Task SearchMore()
    {
        if (!_isSearchingMore)
        {
            _isSearchingMore = true;
            if (OnlineMusicInfoList.HasAllLoaded || !await IsInternetAvailableAsync())
            {
                return;
            }
            IsSearchMoreProgressRingActive = true;
            try
            {
                if (PageIndex == 0)
                {
                    if (MusicLibraryIndex == 0)
                    {
                        if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                        {
                            OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                        }
                        await OnlineMusicInfoList.SearchMore();
                    }
                    else if (MusicLibraryIndex == 1)
                    { }
                    else if (MusicLibraryIndex == 2)
                    { }
                    else if (MusicLibraryIndex == 3)
                    { }
                    else if (MusicLibraryIndex == 4)
                    { }
                    else
                    { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                _isSearchingMore = false;
                IsSearchMoreProgressRingActive = false;
            }
        }
    }

    public async Task UpdateSearchResult()
    {
        if (!string.IsNullOrWhiteSpace(KeyWords))
        {
            if (MusicLibraryIndex == 0)
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
            // 待修改
            else if (MusicLibraryIndex == 1)
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
            else if (MusicLibraryIndex == 2)
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
            else if (MusicLibraryIndex == 3)
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
            else if (MusicLibraryIndex == 4)
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
            else
            {
                if (OnlineMusicInfoList is not CloudBriefOnlineMusicInfoList)
                {
                    OnlineMusicInfoList = new CloudBriefOnlineMusicInfoList();
                }
                SearchResultList = await OnlineMusicInfoList.GetSearchResultAsync(KeyWords);
            }
        }
        else
        {
            ClearSearchResult();
        }
    }

    public void ClearSearchResult()
    {
        SearchResultList = [];
    }

    public void OnlineSongsPlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"OnlineSongs:Part:{KeyWords}", OnlineMusicInfoList, (byte)(MusicLibraryIndex + 1), 0);
        if (sender is Button button && button.DataContext is IBriefOnlineMusicInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void OnlineSongsSongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"OnlineSongs:Part:{KeyWords}", OnlineMusicInfoList, (byte)(MusicLibraryIndex + 1), 0);
        if (e.ClickedItem is IBriefOnlineMusicInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await Search();
    }

    private static async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("https://www.baidu.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
