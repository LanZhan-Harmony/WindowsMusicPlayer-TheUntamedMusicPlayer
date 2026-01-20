using System.Diagnostics;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Helpers;
using ZLogger;

namespace UntamedMusicPlayer.Services;

/// <summary>
/// ZLogger高性能日志扩展方法
/// 使用ZLogger的零分配特性和结构化日志
/// </summary>
public static class ZLoggerExtensions
{
    extension(ILogger logger)
    {
        // 应用程序生命周期日志
        public void ApplicationStarting() => logger.ZLogInformation($"应用程序正在启动");

        public void ApplicationStarted() => logger.ZLogInformation($"应用程序启动完成");

        public void ApplicationShuttingDown() => logger.ZLogInformation($"应用程序正在关闭");

        // 音乐库相关日志
        public void SavingLibraryData(string path) =>
            logger.ZLogInformation($"正在保存音乐库数据到: {path}");

        public void LibraryDataSaved(
            string path,
            double elapsedMs,
            int songCount,
            int albumCount
        ) =>
            logger.ZLogInformation(
                $"音乐库数据已保存到: {path}, 耗时: {elapsedMs}ms, 歌曲数: {songCount}, 专辑数: {albumCount}"
            );

        public void LoadingLibraryData(string path) =>
            logger.ZLogInformation($"正在加载音乐库数据从: {path}");

        public void LibraryDataLoaded(
            string path,
            double elapsedMs,
            int songCount,
            int albumCount
        ) =>
            logger.ZLogInformation(
                $"音乐库数据已加载从: {path}, 耗时: {elapsedMs}ms, 歌曲数: {songCount}, 专辑数: {albumCount}"
            );

        public void LibraryScanning(int processedCount, double progressPercent) =>
            logger.ZLogDebug(
                $"正在扫描音乐库: 已处理 {processedCount} 个文件, 进度: {progressPercent}%"
            );

        // 播放器相关日志
        public void SongStartedPlaying(string title, string artist) =>
            logger.ZLogInformation($"开始播放歌曲: {title} - {artist}");

        public void SongPlaybackError(string title, Exception? exception = null) =>
            logger.ZLogError(
                exception,
                $"{"Error_SongPlayback".GetLocalizedWithReplace("{title}", title)}"
            );

        public void PlaybackDeviceBusy(Exception? exception = null) =>
            logger.ZLogError(exception, $"{"Error_PlaybackDeviceBusy".GetLocalized()}");

        public void SongPlaybackPosition(string title, long positionMs) =>
            logger.ZLogTrace($"歌曲播放位置更新: {title}, 位置: {positionMs}ms");

        // 下载相关日志
        public void DownloadStarted(string title, string url) =>
            logger.ZLogInformation($"开始下载: {title}, URL: {url}");

        public void DownloadCompleted(string title, double elapsedMs, long fileSizeBytes) =>
            logger.ZLogInformation(
                $"下载完成: {title}, 耗时: {elapsedMs}ms, 文件大小: {fileSizeBytes} 字节"
            );

        public void DownloadFailed(string title, string error, Exception? exception = null) =>
            logger.ZLogError(exception, $"下载失败: {title}, 错误: {error}");

        public void DownloadProgress(string title, int progressPercent) =>
            logger.ZLogTrace($"下载进度: {title}, 进度: {progressPercent}%");

        // 用户界面相关日志
        public void NavigationOccurred(string pageName) =>
            logger.ZLogDebug($"页面导航: {pageName}");

        public void UserAction(string actionName) => logger.ZLogDebug($"用户操作: {actionName}");

        // 性能相关日志
        public void PerformanceMetric(string metricName, double value) =>
            logger.ZLogInformation($"性能指标: {metricName}, 值: {value}ms");

        public void MemoryUsage(long memoryBytes, int generation) =>
            logger.ZLogInformation($"内存使用情况: {memoryBytes} 字节, GC代数: {generation}");

        // 错误和异常日志
        public void UnexpectedException(string message, Exception exception) =>
            logger.ZLogError(exception, $"未处理的异常: {message}");

        public void OperationFailed(
            string operationName,
            string error,
            Exception? exception = null
        ) => logger.ZLogError(exception, $"操作失败: {operationName}, 错误: {error}");

        public void CriticalError(string message, Exception? exception = null) =>
            logger.ZLogCritical(exception, $"严重错误: {message}");

        // 歌曲信息编辑日志
        public void EditingSongInfoIO(string title) =>
            logger.ZLogError(
                $"{"Error_EditingSongInfoIO".GetLocalizedWithReplace("{title}", title)}"
            );

        public void EditingSongInfoOther(string title, Exception exception) =>
            logger.ZLogError(
                exception,
                $"{"Error_EditingSongInfoOther".GetLocalizedWithReplace("{title}", title)}"
            );

        // 播放列表相关日志
        public void SamePlaylistName() =>
            logger.ZLogError($"{"Error_SamePlaylistName".GetLocalized()}");

        // 高性能性能监控日志
        public void LogPerformanceStart(string operationName, int operationId) =>
            logger.ZLogDebug($"[{operationId:X8}] 开始执行: {operationName}");

        public void LogPerformanceEnd(string operationName, int operationId, double elapsedMs) =>
            logger.ZLogInformation(
                $"[{operationId:X8}] 完成执行: {operationName}, 耗时: {elapsedMs}ms"
            );

        public void LogPerformanceEnd(string operationName, int operationId, TimeSpan elapsed) =>
            logger.ZLogInformation(
                $"[{operationId:X8}] 完成执行: {operationName}, 耗时: {elapsed.TotalMilliseconds}ms"
            );

        // 音乐播放专用高性能日志
        public void PlaybackStateChanged(string songTitle, string newState) =>
            logger.ZLogDebug($"播放状态变更: {songTitle} -> {newState}");

        public void AudioStreamCreated(string filePath, int streamHandle, double durationSeconds) =>
            logger.ZLogDebug(
                $"音频流已创建: {filePath}, 句柄: {streamHandle}, 时长: {durationSeconds}s"
            );

        public void AudioStreamReleased(int streamHandle) =>
            logger.ZLogDebug($"音频流已释放: 句柄 {streamHandle}");

        public void PlaybackBufferUnderrun(string songTitle, int bufferLevel) =>
            logger.ZLogWarning($"播放缓冲区不足: {songTitle}, 缓冲区水平: {bufferLevel}%");

        // 网络请求日志
        public void HttpRequestStarted(string method, string url, int requestId) =>
            logger.ZLogDebug($"[{requestId:X8}] HTTP请求开始: {method} {url}");

        public void HttpRequestCompleted(int requestId, int statusCode, double elapsedMs) =>
            logger.ZLogInformation(
                $"[{requestId:X8}] HTTP请求完成: {statusCode}, 耗时: {elapsedMs}ms"
            );

        public void HttpRequestFailed(int requestId, string error, Exception? exception = null) =>
            logger.ZLogError(exception, $"[{requestId:X8}] HTTP请求失败: {error}");

        // 文件操作日志
        public void FileOperationStarted(string operation, string filePath, int operationId) =>
            logger.ZLogDebug($"[{operationId:X8}] 文件操作开始: {operation} -> {filePath}");

        public void FileOperationCompleted(int operationId, double elapsedMs, long? fileSize = null)
        {
            if (fileSize.HasValue)
            {
                logger.ZLogInformation(
                    $"[{operationId:X8}] 文件操作完成, 耗时: {elapsedMs}ms, 大小: {fileSize} 字节"
                );
            }
            else
            {
                logger.ZLogInformation($"[{operationId:X8}] 文件操作完成, 耗时: {elapsedMs}ms");
            }
        }

        public void FileOperationFailed(
            int operationId,
            string error,
            Exception? exception = null
        ) => logger.ZLogError(exception, $"[{operationId:X8}] 文件操作失败: {error}");

        // 数据库操作日志
        public void DatabaseOperationStarted(string operation, int operationId) =>
            logger.ZLogDebug($"[{operationId:X8}] 数据库操作开始: {operation}");

        public void DatabaseOperationCompleted(
            int operationId,
            double elapsedMs,
            int? affectedRows = null
        )
        {
            if (affectedRows.HasValue)
            {
                logger.ZLogInformation(
                    $"[{operationId:X8}] 数据库操作完成, 耗时: {elapsedMs}ms, 影响行数: {affectedRows}"
                );
            }
            else
            {
                logger.ZLogInformation($"[{operationId:X8}] 数据库操作完成, 耗时: {elapsedMs}ms");
            }
        }

        public void DatabaseOperationFailed(
            int operationId,
            string error,
            Exception? exception = null
        ) => logger.ZLogError(exception, $"[{operationId:X8}] 数据库操作失败: {error}");
    }
}

/// <summary>
/// 高性能性能监控辅助类
/// 用于自动记录操作的开始和结束时间
/// </summary>
public readonly struct PerformanceScope : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly int _operationId;
    private readonly Stopwatch _stopwatch;

    public PerformanceScope(ILogger logger, string operationName)
    {
        _logger = logger;
        _operationName = operationName;
        _operationId = Random.Shared.Next();
        _stopwatch = Stopwatch.StartNew();

        logger.LogPerformanceStart(operationName, _operationId);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogPerformanceEnd(_operationName, _operationId, _stopwatch.Elapsed);
    }
}

/// <summary>
/// 高性能日志辅助类
/// </summary>
public static class PerformanceLogger
{
    /// <summary>
    /// 创建性能监控作用域
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>性能监控作用域</returns>
    public static PerformanceScope BeginScope(ILogger logger, string operationName) =>
        new(logger, operationName);
}
