using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Storage;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Helpers;

public static class DownloadHelper
{
    private static readonly ILogger _logger = LoggingService.CreateLogger(nameof(DownloadHelper));
    private static readonly HttpClient _sharedHttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5), // 增加超时时间
    };

    private static CancellationTokenSource? _currentDownloadCts;
    private static string? _currentDownloadPath; // 当前下载的临时文件路径
    private static string? _currentFinalPath; // 最终的目标文件路径

    public static async Task DownloadOnlineSongAsync(
        IBriefSongInfoBase info,
        CancellationToken cancellationToken = default
    )
    {
        // 取消之前的下载
        await CancelCurrentDownloadAsync();
        _currentDownloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Data.IsMusicProcessing = true;
        _currentDownloadPath = null;
        _currentFinalPath = null;

        try
        {
            await ShowStartNotificationAsync(info.Title);

            var detailedInfo = (IDetailedOnlineSongInfo)
                await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);

            var (tempPath, finalPath) = await PrepareDownloadPathAsync(detailedInfo);
            _currentDownloadPath = tempPath;
            _currentFinalPath = finalPath;

            // 显示开始通知

            // 使用 IProgress 进行进度报告
            var progress = new Progress<int>(async progressPercentage =>
                await UpdateProgressAsync(detailedInfo.Title, progressPercentage)
            );

            // 执行下载 (最高到99%)
            await DownloadFileWithProgressAsync(
                detailedInfo.Path,
                tempPath,
                progress,
                _currentDownloadCts.Token
            );

            // 重命名文件到最终路径
            await RenameToFinalPathAsync(tempPath, finalPath);

            // 写入歌曲信息 (从99%到100%)
            await WriteSongInfoAsync(finalPath, detailedInfo, _currentDownloadCts.Token);

            // 显示100%完成
            await UpdateProgressAsync(detailedInfo.Title, 100);

            // 显示完成通知
            await ShowCompletionNotificationAsync(detailedInfo.Title, finalPath);

            // 重新加载音乐库
            _ = Data.MusicLibrary.LoadLibraryAgainAsync();
        }
        catch (OperationCanceledException)
        {
            // 用户取消或超时
            await HandleDownloadCancelledAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"下载{info.Title}失败");
            await HandleDownloadErrorAsync(info.Title);
        }
        finally
        {
            Data.IsMusicProcessing = false;
            _currentDownloadCts?.Dispose();
            _currentDownloadCts = null;
            _currentDownloadPath = null;
            _currentFinalPath = null;
        }
    }

    private static async Task<(string tempPath, string finalPath)> PrepareDownloadPathAsync(
        IDetailedOnlineSongInfo detailedInfo
    )
    {
        var location = await LoadSongDownloadLocationAsync();
        var fileName = $"{detailedInfo.Title}{detailedInfo.ItemType}";
        var finalPath = GetUniqueFilePath(Path.Combine(location, fileName));

        // 创建临时文件路径，添加 .crdownload 后缀
        var tempPath = finalPath + ".crdownload";

        return (tempPath, finalPath);
    }

    private static async Task DownloadFileWithProgressAsync(
        string url,
        string savePath,
        IProgress<int> progress,
        CancellationToken cancellationToken
    )
    {
        using var response = await _sharedHttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            savePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true
        );

        var buffer = new byte[8192];
        var totalRead = 0L;
        var lastProgressUpdate = 0;
        var lastUpdateTime = DateTime.UtcNow;

        // 立即报告0%进度
        progress.Report(0);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes.HasValue)
            {
                // 计算进度，但最高只到99%
                var actualProgress = (int)(totalRead * 100 / totalBytes.Value);
                var progressPercentage = Math.Min(actualProgress, 99);
                var now = DateTime.UtcNow;

                // 优化的进度更新逻辑
                if (
                    ShouldUpdateProgress(
                        progressPercentage,
                        lastProgressUpdate,
                        now,
                        lastUpdateTime
                    )
                )
                {
                    lastProgressUpdate = progressPercentage;
                    lastUpdateTime = now;
                    progress.Report(progressPercentage);
                }
            }
        }

        // 下载完成但不报告100%，保持在99%
        progress.Report(99);
    }

    private static async Task RenameToFinalPathAsync(string tempPath, string finalPath)
    {
        try
        {
            // 确保目标文件不存在
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            // 重命名临时文件到最终路径
            await Task.Run(() => File.Move(tempPath, finalPath));
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"文件重命名失败");
            throw;
        }
    }

    /// <summary>
    /// 清理部分下载的文件
    /// </summary>
    private static async Task CleanupPartialDownloadAsync()
    {
        await Task.Run(() =>
        {
            // 清理临时文件 (.crdownload)
            if (!string.IsNullOrEmpty(_currentDownloadPath) && File.Exists(_currentDownloadPath))
            {
                try
                {
                    File.Delete(_currentDownloadPath);
                }
                catch (Exception ex)
                {
                    _logger.ZLogInformation(ex, $"删除临时下载文件失败");
                }
            }

            // 清理最终文件（如果已经重命名但操作失败）
            if (!string.IsNullOrEmpty(_currentFinalPath) && File.Exists(_currentFinalPath))
            {
                try
                {
                    File.Delete(_currentFinalPath);
                }
                catch (Exception ex)
                {
                    _logger.ZLogInformation(ex, $"删除最终下载文件失败");
                }
            }
        });
    }

    /// <summary>
    /// 取消当前下载并清理资源
    /// </summary>
    public static async Task CancelCurrentDownloadAsync()
    {
        if (_currentDownloadCts is null || _currentDownloadCts.Token.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _currentDownloadCts.Cancel();

            // 等待一小段时间让取消操作完成
            await Task.Delay(100);

            // 删除部分下载的文件（包括 .crdownload 和最终文件）
            await CleanupPartialDownloadAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"取消下载失败");
        }
    }

    private static bool ShouldUpdateProgress(
        int current,
        int last,
        DateTime now,
        DateTime lastUpdate
    )
    {
        var timeSinceLastUpdate = (now - lastUpdate).TotalMilliseconds;

        return current > last
            && (
                current - last >= 1 && timeSinceLastUpdate >= 250 // 常规更新：1%且250ms
                || current - last >= 10 // 强制更新：每10%
                || timeSinceLastUpdate >= 1000 // 最长1秒必须更新一次
            );
    }

    public static async Task WriteSongInfoAsync(
        string savePath,
        IDetailedOnlineSongInfo detailedInfo,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // 写入基本属性
            var file = await StorageFile.GetFileFromPathAsync(savePath);
            var musicProperties = await file.Properties.GetMusicPropertiesAsync();

            musicProperties.Title = detailedInfo.Title;
            musicProperties.Album = detailedInfo.Album;
            musicProperties.Artist = detailedInfo.ArtistsStr;
            musicProperties.AlbumArtist = detailedInfo.AlbumArtistsStr;

            if (uint.TryParse(detailedInfo.YearStr, out var year))
            {
                musicProperties.Year = year;
            }

            await musicProperties.SavePropertiesAsync();

            // 写入歌词和封面
            await WriteTagLibInfoAsync(savePath, detailedInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"写入{detailedInfo.Title}歌曲信息失败");
            throw; // 重新抛出异常以便上层处理
        }
    }

    private static async Task WriteTagLibInfoAsync(
        string savePath,
        IDetailedOnlineSongInfo detailedInfo,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var musicFile = TagLib.File.Create(savePath);
            musicFile.Tag.Lyrics = detailedInfo.Lyric;

            // 下载并设置封面
            if (!string.IsNullOrEmpty(detailedInfo.CoverPath))
            {
                await SetAlbumCoverAsync(musicFile, detailedInfo.CoverPath, cancellationToken);
            }

            musicFile.Save();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"写入{detailedInfo.Title}TagLib信息失败");
            throw; // 重新抛出异常以便上层处理
        }
    }

    private static async Task SetAlbumCoverAsync(
        TagLib.File musicFile,
        string coverUrl,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var imageBytes = await _sharedHttpClient.GetByteArrayAsync(coverUrl, cancellationToken);
            var picture = new TagLib.Picture(imageBytes);
            musicFile.Tag.Pictures = [picture];
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"设置封面失败");
        }
    }

    /// <summary>
    /// 处理下载被取消
    /// </summary>
    private static async Task HandleDownloadCancelledAsync()
    {
        await CleanupPartialDownloadAsync();
        await ShowCancelNotificationAsync();
        await AppNotificationManager.Default.RemoveAllAsync();
    }

    /// <summary>
    /// 处理下载错误
    /// </summary>
    private static async Task HandleDownloadErrorAsync(string title)
    {
        await CleanupPartialDownloadAsync();
        await ShowErrorNotificationAsync(title);
        await AppNotificationManager.Default.RemoveAllAsync();
    }

    // 通知相关方法
    private static async Task ShowStartNotificationAsync(string title)
    {
        var notification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadingSong".GetLocalized())
            .AddProgressBar(
                new AppNotificationProgressBar { Title = title }
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
            ValueStringOverride = $"{"OnlineMusicLibrary_Progress".GetLocalized()}: 0%",
            Status = "OnlineMusicLibrary_StartDownloading".GetLocalized(),
        };

        notification.Tag = "OnlineMusicDownload";
        notification.Group = "Downloads";
        notification.Progress = data;

        await AppNotificationManager.Default.RemoveAllAsync();
        AppNotificationManager.Default.Show(notification);
    }

    private static async Task UpdateProgressAsync(string title, int progress)
    {
        var data = new AppNotificationProgressData(2)
        {
            Title = title,
            Value = progress / 100.0,
            ValueStringOverride = $"{"OnlineMusicLibrary_Progress".GetLocalized()}: {progress}%",
            Status =
                progress < 99
                    ? "OnlineMusicLibrary_Downloading".GetLocalized()
                    : "OnlineMusicLibrary_Processing".GetLocalized(), // 99%时显示"正在处理"
        };

        await AppNotificationManager.Default.UpdateAsync(data, "OnlineMusicDownload", "Downloads");
    }

    private static async Task ShowCompletionNotificationAsync(string title, string savePath)
    {
        await AppNotificationManager.Default.RemoveAllAsync();

        var notification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadCompleted".GetLocalized())
            .AddText(title)
            .AddButton(
                new AppNotificationButton(
                    "OnlineMusicLibrary_OpenFolder".GetLocalized()
                ).AddArgument("OpenFolderAction", savePath)
            )
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    private static async Task ShowCancelNotificationAsync()
    {
        await AppNotificationManager.Default.RemoveAllAsync();

        var notification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadCancelled".GetLocalized())
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    private static async Task ShowErrorNotificationAsync(string title)
    {
        await AppNotificationManager.Default.RemoveAllAsync();

        var notification = new AppNotificationBuilder()
            .AddText("OnlineMusicLibrary_DownloadFailed".GetLocalized())
            .AddText(title)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    public static void CancelCurrentDownload()
    {
        _ = CancelCurrentDownloadAsync();
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var count = 1; ; count++)
        {
            var uniquePath = Path.Combine(directory, $"{fileNameWithoutExt}({count}){extension}");
            if (!File.Exists(uniquePath))
            {
                return uniquePath;
            }
        }
    }

    private static async Task<string> LoadSongDownloadLocationAsync()
    {
        var localSettingsService = App.GetService<ILocalSettingsService>();
        var location = await localSettingsService.ReadSettingAsync<string>("SongDownloadLocation");

        if (!string.IsNullOrWhiteSpace(location))
        {
            return location;
        }

        // 获取默认音乐文件夹
        try
        {
            var musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            location = musicLibrary.Folders.AsValueEnumerable().FirstOrDefault()?.Path;
        }
        catch { }

        if (string.IsNullOrWhiteSpace(location))
        {
            location = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Music"
            );
            Directory.CreateDirectory(location);
        }

        return location;
    }
}
