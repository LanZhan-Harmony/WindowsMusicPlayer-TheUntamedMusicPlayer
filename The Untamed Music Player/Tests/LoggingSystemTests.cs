using Microsoft.Extensions.Logging;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Tests;

/// <summary>
/// 日志系统测试和演示类
/// </summary>
public static class LoggingSystemTests
{
    private static readonly ILogger _logger = LoggingService.CreateLogger("LoggingTests");

    /// <summary>
    /// 测试和演示日志系统的各种功能
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        _logger.LogInformation("开始执行日志系统测试");

        // 测试基本日志级别
        TestBasicLogging();

        // 测试结构化日志
        TestStructuredLogging();

        // 测试异常处理日志
        TestExceptionLogging();

        // 测试高性能日志记录
        TestHighPerformanceLogging();

        // 测试性能监控
        await TestPerformanceMonitoringAsync();

        // 测试条件日志记录
        TestConditionalLogging();

        // 测试InfoBar显示（这会在UI中显示）
        TestInfoBarLogging();

        _logger.LogInformation("日志系统测试完成");
    }

    private static void TestBasicLogging()
    {
        _logger.LogDebug("这是调试信息 - 只在Debug模式下显示");
        _logger.LogInformation("这是普通信息日志");
        _logger.LogWarning("这是警告信息");

        // 注意：以下两条日志会在InfoBar中显示
        // _logger.LogError("这是错误信息 - 会在InfoBar中显示");
        // _logger.LogCritical("这是严重错误 - 会在InfoBar中显示");
    }

    private static void TestStructuredLogging()
    {
        var userId = 12345;
        var userName = "张三";
        var songTitle = "测试歌曲";
        var playTime = TimeSpan.FromMinutes(3.5);

        _logger.LogInformation(
            "用户 {UserId} ({UserName}) 播放了歌曲 {SongTitle}，时长 {Duration}",
            userId,
            userName,
            songTitle,
            playTime
        );

        var sessionInfo = new
        {
            SessionId = Guid.NewGuid(),
            StartTime = DateTime.Now,
            UserAgent = "The Untamed Music Player/1.0",
        };

        _logger.LogInformation("用户会话信息: {@SessionInfo}", sessionInfo);
    }

    private static void TestExceptionLogging()
    {
        try
        {
            // 模拟一个异常
            throw new InvalidOperationException("这是一个测试异常");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "捕获到测试异常，操作参数: {Parameter}", "test_value");

            // 使用高性能日志记录器
            _logger.UnexpectedException("测试异常处理", ex);
        }
    }

    private static void TestHighPerformanceLogging()
    {
        // 使用预编译的高性能日志模板
        _logger.SongStartedPlaying("测试歌曲", "测试艺术家");
        _logger.DownloadProgress("测试下载", 75);
        _logger.PerformanceMetric("测试操作", 1250.5);
        _logger.MemoryUsage(1024 * 1024 * 50, 2); // 50MB, Gen 2
    }

    private static async Task TestPerformanceMonitoringAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationName = "性能测试操作";

        try
        {
            _logger.LogInformation("开始执行 {OperationName}", operationName);

            // 模拟一些工作
            await Task.Delay(500);

            stopwatch.Stop();
            _logger.PerformanceMetric(operationName, stopwatch.Elapsed.TotalMilliseconds);

            _logger.LogInformation(
                "{OperationName} 执行完成，耗时: {ElapsedMs}ms",
                operationName,
                stopwatch.Elapsed.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.OperationFailed(operationName, ex.Message, ex);
        }
    }

    private static void TestConditionalLogging()
    {
        // 高效的条件日志记录
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var expensiveDebugInfo =
                $"调试信息 - 时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}, 线程: {Environment.CurrentManagedThreadId}";
            _logger.LogDebug("详细调试信息: {DebugInfo}", expensiveDebugInfo);
        }

        // 这个在Release版本中不会执行
        _logger.LogDebug("这条调试信息在Release版本中不会被记录");
    }

    private static void TestInfoBarLogging()
    {
        _logger.LogInformation("准备测试InfoBar显示功能...");

        // 等待一下再显示错误，这样可以看到准备信息
        Task.Delay(2000)
            .ContinueWith(_ =>
            {
                // 这条错误日志会在MainWindow的InfoBar中显示
                _logger.LogError("这是一个测试错误消息，应该会在InfoBar中显示5秒钟");
            });

        Task.Delay(4000)
            .ContinueWith(_ =>
            {
                // 再显示一条严重错误
                _logger.LogCritical("这是一个严重错误测试，也会在InfoBar中显示");
            });
    }

    /// <summary>
    /// 在应用启动时调用此方法来演示日志系统
    /// </summary>
    public static void DemonstrateLoggingOnStartup()
    {
        _logger.LogInformation("日志系统演示：应用程序已启动");
        _logger.LogDebug("调试模式信息：当前时间 {CurrentTime}", DateTime.Now);

        // 记录系统信息
        _logger.LogInformation(
            "系统信息 - OS: {OS}, .NET版本: {DotNetVersion}, 工作目录: {WorkingDirectory}",
            Environment.OSVersion,
            Environment.Version,
            Environment.CurrentDirectory
        );
    }

    /// <summary>
    /// 在应用关闭时调用此方法
    /// </summary>
    public static void DemonstrateLoggingOnShutdown()
    {
        _logger.LogInformation("日志系统演示：应用程序正在关闭");

        // 记录一些关闭统计信息
        var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
        _logger.LogInformation("应用程序运行时间: {Uptime}", uptime);
    }
}
