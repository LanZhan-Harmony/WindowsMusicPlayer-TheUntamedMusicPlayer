using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using Windows.UI.Notifications;

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
        if (!_isSearchingMore && !OnlineMusicInfoList.HasAllLoaded)
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
                        await OnlineMusicInfoList.SearchMoreAsync();
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

    public void OnlineSongsSongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList($"OnlineSongs:Part:{KeyWords}", OnlineMusicInfoList, (byte)(MusicLibraryIndex + 1), 0);
        if (e.ClickedItem is IBriefOnlineMusicInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void OnlineSongsPlayButton_Click(IBriefOnlineMusicInfo info)
    {
        Data.MusicPlayer.SetPlayList($"OnlineSongs:Part:{KeyWords}", OnlineMusicInfoList, (byte)(MusicLibraryIndex + 1), 0);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void OnlineSongsPlayNextButton_Click(IBriefOnlineMusicInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineMusicInfo> { info };
            Data.MusicPlayer.SetPlayList("LocalSongs:Part", list
                , 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public async void OnlineSongsDownloadButton_Click(IBriefOnlineMusicInfo info)
    {
        var detailedInfo = (IDetailedOnlineMusicInfo)await MusicPlayer.CreateDetailedMusicInfoAsync(info, (byte)(MusicLibraryIndex + 1));
        var title = detailedInfo.Title;
        var itemType = detailedInfo.ItemType;
        var savePath = "C:/Users/Admin/音乐/" + title + itemType;
        var tag = "OnlineMusicDownload";
        var group = "Downloads";
        var startNotification = new AppNotificationBuilder()
            .AddProgressBar(new AppNotificationProgressBar() { Title = title }
            .BindValue()
            .BindValueStringOverride()
            .BindStatus()
            )
            .AddButton(new AppNotificationButton("取消")
            .AddArgument("action", "Cancel")
            )
            .SetHeroImage(detailedInfo.CoverUrl != null ? new Uri(detailedInfo.CoverUrl) : null)
            .BuildNotification();
        var data = new AppNotificationProgressData(1)
        {
            Title = title,
            Value = 0,
            ValueStringOverride = "下载进度: 0 %",
            Status = "正在下载"
        };
        startNotification.Tag = tag;
        startNotification.Group = group;
        startNotification.Progress = data;
        AppNotificationManager.Default.Show(startNotification);

        await DownloadFileWithProgress(detailedInfo.Path, savePath, title);

        var finishNotification = new AppNotificationBuilder()
            .AddText("下载完成")
            .AddText(title)
            .BuildNotification();
        AppNotificationManager.Default.Show(finishNotification);
    }

    private static async Task DownloadFileWithProgress(string url, string savePath, string title)
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromMinutes(1) };
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalRead = 0L;
        var read = 0;
        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            if (totalBytes.HasValue)
            {
                var progress = (int)(totalRead * 100 / totalBytes.Value);
                UpdateProgress(title, progress);
            }
        }
    }

    public static async void UpdateProgress(string title, int progress)
    {
        var tag = "OnlineMusicDownload";
        var group = "Downloads";
        var data = new AppNotificationProgressData(2)
        {
            Title = title,
            Value = progress,
            ValueStringOverride = $"下载进度: {progress} %",
            Status = "正在下载"
        };
        var result = await AppNotificationManager.Default.UpdateAsync(data, tag, group);
        if (result == AppNotificationProgressResult.AppNotificationNotFound)
        {
            return;
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
