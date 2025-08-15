using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using The_Untamed_Music_Player.Messages;
using Windows.Storage;

namespace The_Untamed_Music_Player.Services;

/// <summary>
/// 高性能日志服务
/// </summary>
public static class LoggingService
{
    private static ILoggerFactory? _loggerFactory;
    private static readonly Lock _lock = new();

    /// <summary>
    /// 日志工厂实例
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        get
        {
            if (_loggerFactory is null)
            {
                lock (_lock)
                {
                    _loggerFactory ??= CreateLoggerFactory();
                }
            }
            return _loggerFactory;
        }
    }

    /// <summary>
    /// 初始化日志服务
    /// </summary>
    public static void Initialize()
    {
        // 触发日志工厂创建
        _ = LoggerFactory;

        // 启动定期清理任务
        StartPeriodicCleanup();
    }

    /// <summary>
    /// 创建日志记录器
    /// </summary>
    public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    /// <summary>
    /// 创建日志记录器
    /// </summary>
    public static ILogger CreateLogger(string categoryName) =>
        LoggerFactory.CreateLogger(categoryName);

    /// <summary>
    /// 关闭日志服务
    /// </summary>
    public static void Shutdown()
    {
        _loggerFactory?.Dispose();
        NLog.LogManager.Shutdown();
    }

    /// <summary>
    /// 创建日志工厂
    /// </summary>
    private static ILoggerFactory CreateLoggerFactory()
    {
        var logFolder = GetLogFolderPath();
        var logFilePath = Path.Combine(logFolder, "app-${shortdate}.log");

        // 确保日志文件夹存在
        Directory.CreateDirectory(logFolder);

        // 配置 NLog
        var config = new LoggingConfiguration();

        var fileTarget = new FileTarget("fileTarget")
        {
            FileName = logFilePath,
            Layout =
                "${longdate} [${level:uppercase=true:padding=5}] ${logger:shortName=true} ${message} ${exception:format=tostring}",
            MaxArchiveFiles = 7, // 保留7天的日志
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveFileName = Path.Combine(logFolder, "app-{#}.log"), // 归档模式
            ArchiveSuffixFormat = "{0:yyyyMMdd}",
            KeepFileOpen = true,
            AutoFlush = false, // 提高性能
            OpenFileFlushTimeout = 5, // 5秒自动刷新
            CreateDirs = true,
            BufferSize = 32768, // 32KB缓冲区
        };

#if DEBUG
        // 调试输出目标
        var debugTarget = new DebugTarget("debugTarget")
        {
            Layout =
                "${time} [${level:uppercase=true:padding=5}] ${logger:shortName=true} ${message}",
        };
        config.AddTarget(debugTarget);
        config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, debugTarget);
#endif

        config.AddTarget(fileTarget);

        // 所有级别写入文件
        config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget);

        NLog.LogManager.Configuration = config;

        // 创建自定义日志提供程序
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
        });

        // 添加Messenger日志提供程序
        loggerFactory.AddProvider(new MessengerLoggerProvider());

        return loggerFactory;
    }

    /// <summary>
    /// 获取日志文件夹路径
    /// </summary>
    public static string GetLogFolderPath()
    {
        try
        {
            // 尝试使用应用数据文件夹
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            return Path.Combine(localFolder, "Logs");
        }
        catch
        {
            // 回退到用户本地应用数据文件夹
            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            );
            return Path.Combine(localAppData, "The Untamed Music Player", "Logs");
        }
    }

    /// <summary>
    /// 启动定期清理任务
    /// </summary>
    private static void StartPeriodicCleanup()
    {
        // 立即执行一次清理
        _ = Task.Run(CleanupOldLogFiles);

        // 每天执行一次清理
        var timer = new Timer(
            _ => Task.Run(CleanupOldLogFiles),
            null,
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(24)
        );
    }

    /// <summary>
    /// 清理7天前的日志文件
    /// </summary>
    private static void CleanupOldLogFiles()
    {
        try
        {
            var logFolder = GetLogFolderPath();
            if (!Directory.Exists(logFolder))
            {
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-7);
            var logFiles = Directory.GetFiles(logFolder, "*.log*");

            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 忽略删除失败的文件
                    }
                }
            }
        }
        catch
        {
            // 忽略清理过程中的异常
        }
    }
}

/// <summary>
/// Messenger日志提供程序
/// </summary>
internal partial class MessengerLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new MessengerLogger(categoryName);

    public void Dispose() { }
}

/// <summary>
/// Messenger日志记录器
/// </summary>
internal class MessengerLogger(string categoryName) : ILogger
{
    private readonly string _categoryName = categoryName;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (!string.IsNullOrEmpty(message) || exception is not null)
        {
            var fullMessage = exception is not null ? $"{message} {exception.Message}" : message;
            StrongReferenceMessenger.Default.Send(new LogMessage(logLevel, fullMessage));
        }
    }
}
