using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using TagLib;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;
using Windows.Storage;

namespace The_Untamed_Music_Player.Models;

public partial class OnlineMusicLibrary : ObservableRecipient
{
    private const string _tag = "OnlineMusicDownload";
    private const string _group = "Downloads";

    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    private bool _isSearchingMore = false;

    /// <summary>
    /// 页面索引, 0为歌曲, 1为专辑, 2为艺术家, 3为歌单
    /// </summary>
    public byte PageIndex { get; set; }

    /// <summary>
    /// 乐库索引, 0为网易云音乐
    /// </summary>
    public byte MusicLibraryIndex { get; set; }

    public string SuggestKeyWords { get; set; } = null!;
    public string SearchKeyWords { get; set; } = null!;

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
    public partial IBriefOnlineSongInfoList OnlineSongInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial IOnlineAlbumInfoList OnlineAlbumInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial IOnlineArtistInfoList OnlineArtistInfoList { get; set; } = null!;

    [ObservableProperty]
    public partial List<SuggestResult> SuggestResultList { get; set; } = [];

    public async Task Search()
    {
        KeyWordsTextBlockVisibility = Visibility.Collapsed;
        NetworkErrorVisibility = Visibility.Collapsed;
        ListViewOpacity = 0;

        if (string.IsNullOrWhiteSpace(SearchKeyWords))
        {
            return;
        }

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
                switch (MusicLibraryIndex)
                {
                    case 0:
                        var cloudList = OnlineSongInfoList as CloudBriefOnlineSongInfoList ?? [];
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

            KeyWordsText = SearchKeyWords;
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
        if (!_isSearchingMore && !OnlineSongInfoList.HasAllLoaded)
        {
            _isSearchingMore = true;
            if (OnlineSongInfoList.HasAllLoaded || !await IsInternetAvailableAsync())
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
                            var cloudList =
                                OnlineSongInfoList as CloudBriefOnlineSongInfoList ?? [];
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
                    var cloudList = OnlineSongInfoList as CloudBriefOnlineSongInfoList ?? [];
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

    public void OnlineSongsSongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Part:{SearchKeyWords}",
            OnlineSongInfoList,
            (byte)(MusicLibraryIndex + 1),
            0
        );
        if (e.ClickedItem is IBriefOnlineSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void OnlineSongsPlayButton_Click(IBriefOnlineSongInfo info)
    {
        Data.MusicPlayer.SetPlayList(
            $"OnlineSongs:Part:{SearchKeyWords}",
            OnlineSongInfoList,
            (byte)(MusicLibraryIndex + 1),
            0
        );
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void OnlineSongsPlayNextButton_Click(IBriefOnlineSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<IBriefOnlineSongInfo> { info };
            Data.MusicPlayer.SetPlayList("LocalSongs:Part", list, 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public async void OnlineSongsDownloadButton_Click(IBriefOnlineSongInfo info)
    {
        Data.IsMusicDownloading = true;
        var detailedInfo = (IDetailedOnlineSongInfo)
            await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(
                info,
                (byte)(MusicLibraryIndex + 1)
            );
        var title = detailedInfo.Title;
        var itemType = detailedInfo.ItemType;
        var location = await LoadSongDownloadLocationAsync();
        var savePath = GetUniqueFilePath(Path.Combine(location, title + itemType));
        var startNotification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadingSong".GetLocalized())
            .AddProgressBar(
                new AppNotificationProgressBar() { Title = title }
                    .BindValue()
                    .BindValueStringOverride()
                    .BindStatus()
            )
            .AddButton(
                new AppNotificationButton("OnlineMusicLibrary_Cancel".GetLocalized()).AddArgument(
                    "CancelAction",
                    "Cancel"
                )
            )
            .BuildNotification();
        var data = new AppNotificationProgressData(1)
        {
            Title = title,
            Value = 0,
            ValueStringOverride = $"{"OnlineMusicLibrary_Progress".GetLocalized()}: 0 %",
            Status = "OnlineMusicLibrary_StartDownloading".GetLocalized(),
        };
        startNotification.Tag = _tag;
        startNotification.Group = _group;
        startNotification.Progress = data;
        await AppNotificationManager.Default.RemoveAllAsync();
        AppNotificationManager.Default.Show(startNotification);

        try
        {
            await DownloadFileWithProgress(detailedInfo.Path, savePath, title);
        }
        catch
        {
            var errorNotification = new AppNotificationBuilder()
                .AddText("OnlineMusicLibrary_DownloadFailed".GetLocalized())
                .AddText(title)
                .BuildNotification();
            AppNotificationManager.Default.Show(errorNotification);
            return;
        }
        await WriteSongInfo(savePath, detailedInfo);

        Data.IsMusicDownloading = false;

        await AppNotificationManager.Default.RemoveAllAsync();
        var finishNotification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadCompleted".GetLocalized())
            .AddText(title)
            .AddButton(
                new AppNotificationButton(
                    "OnlineMusicLibrary_OpenFolder".GetLocalized()
                ).AddArgument("OpenFolderAction", savePath)
            )
            .BuildNotification();
        AppNotificationManager.Default.Show(finishNotification);
        _ = Data.MusicLibrary.LoadLibraryAgainAsync();
    }

    private static async Task DownloadFileWithProgress(string url, string savePath, string title)
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromMinutes(1) };
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(
            savePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            true
        );

        var buffer = new byte[8192];
        var totalRead = 0L;
        var read = 0;
        var lastProgressUpdate = 0;
        var lastUpdateTime = DateTime.Now;

        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            if (totalBytes.HasValue)
            {
                var progress = (int)(totalRead * 100 / totalBytes.Value);

                // 仅在进度变化至少1%且距上次更新至少100ms时更新UI
                var now = DateTime.Now;
                if (
                    progress > lastProgressUpdate
                    && (
                        progress - lastProgressUpdate >= 1
                        || (now - lastUpdateTime).TotalMilliseconds > 500
                    )
                )
                {
                    lastProgressUpdate = progress;
                    lastUpdateTime = now;
                    UpdateProgress(title, progress);
                }
            }
        }
    }

    public static async Task WriteSongInfo(string savePath, IDetailedOnlineSongInfo detailedInfo)
    {
        try
        {
            var musicFile = TagLib.File.Create(savePath);
            musicFile.Tag.Title = detailedInfo.Title;
            musicFile.Tag.Album = detailedInfo.Album;
            musicFile.Tag.Performers = detailedInfo.ArtistsStr.Split(", ");
            musicFile.Tag.AlbumArtists = detailedInfo.AlbumArtistsStr.Split(", ");
            musicFile.Tag.Year = string.IsNullOrEmpty(detailedInfo.YearStr)
                ? 0
                : uint.Parse(detailedInfo.YearStr);
            musicFile.Tag.Lyrics = detailedInfo.Lyric;
            try
            {
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(detailedInfo.CoverPath);
                var picture = new Picture([.. imageBytes])
                {
                    Type = PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = "image/png", // 若图片是png，则使用 "image/png"
                };
                musicFile.Tag.Pictures = [picture];
            }
            catch
            {
                Debug.WriteLine("获取封面失败");
            }
            musicFile.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public static async void UpdateProgress(string title, int progress)
    {
        var data = new AppNotificationProgressData(2)
        {
            Title = title,
            Value = (double)progress / 100,
            ValueStringOverride = $"{"OnlineMusicLibrary_Progress".GetLocalized()}: {progress} %",
            Status = "OnlineMusicLibrary_Downloading".GetLocalized(),
        };
        var result = await AppNotificationManager.Default.UpdateAsync(data, _tag, _group);
        if (result == AppNotificationProgressResult.AppNotificationNotFound)
        {
            return;
        }
    }

    public void OnlineAlbumsAlbumGridView_ItemClick(object sender, ItemClickEventArgs e) { }

    public async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await Search();
    }

    public static async Task<bool> IsInternetAvailableAsync()
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

    private static string GetUniqueFilePath(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var count = 1;
        var uniquePath = path;
        while (System.IO.File.Exists(uniquePath))
        {
            uniquePath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            count++;
        }
        return uniquePath;
    }

    private async Task<string> LoadSongDownloadLocationAsync()
    {
        var location = await _localSettingsService.ReadSettingAsync<string>("SongDownloadLocation");
        if (string.IsNullOrWhiteSpace(location))
        {
            var folder = (
                await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music)
            ).Folders.FirstOrDefault();
            location = folder?.Path;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Music"
                );
                Directory.CreateDirectory(location);
            }
        }
        return location;
    }
}
