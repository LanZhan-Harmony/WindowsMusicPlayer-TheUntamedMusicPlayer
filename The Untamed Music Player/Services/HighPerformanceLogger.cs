using Microsoft.Extensions.Logging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Services;

/// <summary>
/// 高性能日志记录器，使用预编译的日志消息模板
/// </summary>
public static class HighPerformanceLogger
{
    // 应用程序生命周期日志
    private static readonly Action<ILogger, Exception?> _applicationStarting = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1000, nameof(ApplicationStarting)),
        "应用程序正在启动"
    );

    private static readonly Action<ILogger, Exception?> _applicationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1001, nameof(ApplicationStarted)),
        "应用程序启动完成"
    );

    private static readonly Action<ILogger, Exception?> _applicationShuttingDown =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1002, nameof(ApplicationShuttingDown)),
            "应用程序正在关闭"
        );

    // 音乐库相关日志
    private static readonly Action<ILogger, string, Exception?> _savingLibraryData =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2000, nameof(SavingLibraryData)),
            "正在保存音乐库数据到: {Path}"
        );

    private static readonly Action<
        ILogger,
        string,
        double,
        int,
        int,
        Exception?
    > _libraryDataSaved = LoggerMessage.Define<string, double, int, int>(
        LogLevel.Information,
        new EventId(2001, nameof(LibraryDataSaved)),
        "音乐库数据已保存到: {Path}, 耗时: {ElapsedMs}ms, 歌曲数: {SongCount}, 专辑数: {AlbumCount}"
    );

    private static readonly Action<ILogger, string, Exception?> _loadingLibraryData =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2002, nameof(LoadingLibraryData)),
            "正在加载音乐库数据从: {Path}"
        );

    private static readonly Action<
        ILogger,
        string,
        double,
        int,
        int,
        Exception?
    > _libraryDataLoaded = LoggerMessage.Define<string, double, int, int>(
        LogLevel.Information,
        new EventId(2003, nameof(LibraryDataLoaded)),
        "音乐库数据已加载从: {Path}, 耗时: {ElapsedMs}ms, 歌曲数: {SongCount}, 专辑数: {AlbumCount}"
    );

    private static readonly Action<ILogger, int, double, Exception?> _libraryScanning =
        LoggerMessage.Define<int, double>(
            LogLevel.Debug,
            new EventId(2004, nameof(LibraryScanning)),
            "正在扫描音乐库: 已处理 {ProcessedCount} 个文件, 进度: {ProgressPercent}%"
        );

    // 编辑歌曲信息相关日志
    private static readonly Action<ILogger, string, Exception?> _editingSongInfoIO =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2500, nameof(EditingSongInfoIO)),
            "Error_EditingSongInfoIO".GetLocalized()
        );

    private static readonly Action<ILogger, string, Exception?> _editingSongInfoOther =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2501, nameof(EditingSongInfoOther)),
            "Error_EditingSongInfoOther".GetLocalized()
        );

    // 播放器相关日志
    private static readonly Action<ILogger, string, string, Exception?> _songStartedPlaying =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3000, nameof(SongStartedPlaying)),
            "开始播放歌曲: {Title} - {Artist}"
        );

    private static readonly Action<ILogger, string, Exception?> _songPlaybackError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3001, nameof(SongPlaybackError)),
            "Error_SongPlayback".GetLocalized()
        );

    private static readonly Action<ILogger, string, long, Exception?> _songPlaybackPosition =
        LoggerMessage.Define<string, long>(
            LogLevel.Debug,
            new EventId(3002, nameof(SongPlaybackPosition)),
            "歌曲播放位置更新: {Title}, 位置: {PositionMs}ms"
        );

    // 下载相关日志
    private static readonly Action<ILogger, string, string, Exception?> _downloadStarted =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(4000, nameof(DownloadStarted)),
            "开始下载: {Title}, URL: {Url}"
        );

    private static readonly Action<ILogger, string, double, long, Exception?> _downloadCompleted =
        LoggerMessage.Define<string, double, long>(
            LogLevel.Information,
            new EventId(4001, nameof(DownloadCompleted)),
            "下载完成: {Title}, 耗时: {ElapsedMs}ms, 文件大小: {FileSizeBytes} 字节"
        );

    private static readonly Action<ILogger, string, string, Exception?> _downloadFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(4002, nameof(DownloadFailed)),
            "下载失败: {Title}, 错误: {Error}"
        );

    private static readonly Action<ILogger, string, int, Exception?> _downloadProgress =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(4003, nameof(DownloadProgress)),
            "下载进度: {Title}, 进度: {ProgressPercent}%"
        );

    // 用户界面相关日志
    private static readonly Action<ILogger, string, Exception?> _navigationOccurred =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(5000, nameof(NavigationOccurred)),
            "页面导航: {PageName}"
        );

    private static readonly Action<ILogger, string, Exception?> _userAction =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(5001, nameof(UserAction)),
            "用户操作: {ActionName}"
        );

    // 性能相关日志
    private static readonly Action<ILogger, string, double, Exception?> _performanceMetric =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(6000, nameof(PerformanceMetric)),
            "性能指标: {MetricName}, 值: {Value}ms"
        );

    private static readonly Action<ILogger, long, int, Exception?> _memoryUsage =
        LoggerMessage.Define<long, int>(
            LogLevel.Information,
            new EventId(6001, nameof(MemoryUsage)),
            "内存使用情况: {MemoryBytes} 字节, GC代数: {Generation}"
        );

    // 错误和异常日志
    private static readonly Action<ILogger, string, Exception?> _unexpectedException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9000, nameof(UnexpectedException)),
            "未处理的异常: {Message}"
        );

    private static readonly Action<ILogger, string, string, Exception?> _operationFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(9001, nameof(OperationFailed)),
            "操作失败: {OperationName}, 错误: {Error}"
        );

    private static readonly Action<ILogger, string, Exception?> _criticalError =
        LoggerMessage.Define<string>(
            LogLevel.Critical,
            new EventId(9999, nameof(CriticalError)),
            "严重错误: {Message}"
        );

    // 应用程序生命周期方法
    public static void ApplicationStarting(this ILogger logger) =>
        _applicationStarting(logger, null);

    public static void ApplicationStarted(this ILogger logger) => _applicationStarted(logger, null);

    public static void ApplicationShuttingDown(this ILogger logger) =>
        _applicationShuttingDown(logger, null);

    // 音乐库相关方法
    public static void SavingLibraryData(this ILogger logger, string path) =>
        _savingLibraryData(logger, path, null);

    public static void LibraryDataSaved(
        this ILogger logger,
        string path,
        double elapsedMs,
        int songCount,
        int albumCount
    ) => _libraryDataSaved(logger, path, elapsedMs, songCount, albumCount, null);

    public static void LoadingLibraryData(this ILogger logger, string path) =>
        _loadingLibraryData(logger, path, null);

    public static void LibraryDataLoaded(
        this ILogger logger,
        string path,
        double elapsedMs,
        int songCount,
        int albumCount
    ) => _libraryDataLoaded(logger, path, elapsedMs, songCount, albumCount, null);

    public static void LibraryScanning(
        this ILogger logger,
        int processedCount,
        double progressPercent
    ) => _libraryScanning(logger, processedCount, progressPercent, null);

    // 编辑歌曲信息相关方法
    public static void EditingSongInfoIO(
        this ILogger logger,
        string title,
        Exception? exception = null
    ) => _editingSongInfoIO(logger, title, exception);

    public static void EditingSongInfoOther(
        this ILogger logger,
        string title,
        Exception? exception = null
    ) => _editingSongInfoOther(logger, title, exception);

    // 播放器相关方法
    public static void SongStartedPlaying(this ILogger logger, string title, string artist) =>
        _songStartedPlaying(logger, title, artist, null);

    public static void SongPlaybackError(
        this ILogger logger,
        string title,
        Exception? exception = null
    ) => _songPlaybackError(logger, title, exception);

    public static void SongPlaybackPosition(this ILogger logger, string title, long positionMs) =>
        _songPlaybackPosition(logger, title, positionMs, null);

    // 下载相关方法
    public static void DownloadStarted(this ILogger logger, string title, string url) =>
        _downloadStarted(logger, title, url, null);

    public static void DownloadCompleted(
        this ILogger logger,
        string title,
        double elapsedMs,
        long fileSizeBytes
    ) => _downloadCompleted(logger, title, elapsedMs, fileSizeBytes, null);

    public static void DownloadFailed(
        this ILogger logger,
        string title,
        string error,
        Exception? exception = null
    ) => _downloadFailed(logger, title, error, exception);

    public static void DownloadProgress(this ILogger logger, string title, int progressPercent) =>
        _downloadProgress(logger, title, progressPercent, null);

    // 用户界面相关方法
    public static void NavigationOccurred(this ILogger logger, string pageName) =>
        _navigationOccurred(logger, pageName, null);

    public static void UserAction(this ILogger logger, string actionName) =>
        _userAction(logger, actionName, null);

    // 性能相关方法
    public static void PerformanceMetric(this ILogger logger, string metricName, double value) =>
        _performanceMetric(logger, metricName, value, null);

    public static void MemoryUsage(this ILogger logger, long memoryBytes, int generation) =>
        _memoryUsage(logger, memoryBytes, generation, null);

    // 错误和异常方法
    public static void UnexpectedException(
        this ILogger logger,
        string message,
        Exception exception
    ) => _unexpectedException(logger, message, exception);

    public static void OperationFailed(
        this ILogger logger,
        string operationName,
        string error,
        Exception? exception = null
    ) => _operationFailed(logger, operationName, error, exception);

    public static void CriticalError(
        this ILogger logger,
        string message,
        Exception? exception = null
    ) => _criticalError(logger, message, exception);
}
