# 日志系统使用指南

本项目使用 Microsoft.Extensions.Logging + NLog.Extensions.Logging 构建了一个高性能的日志系统，支持异步写入、自动清理和InfoBar显示。

## 特性

- ✅ **高性能异步写入**：使用NLog的异步目标，不阻塞主线程
- ✅ **自动文件管理**：自动清理7天前的日志文件
- ✅ **InfoBar集成**：Error和Critical级别的日志会自动在MainWindow的InfoBar中显示5秒
- ✅ **Release和AOT兼容**：在Release版本和AOT环境下正常工作
- ✅ **结构化日志**：支持结构化日志记录，便于查询和分析
- ✅ **高性能模板**：使用LoggerMessage.Define预编译消息模板

## 基本使用

### 1. 获取日志记录器

```csharp
// 在服务或ViewModel中
using Microsoft.Extensions.Logging;
using The_Untamed_Music_Player.Services;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService()
    {
        _logger = LoggingService.CreateLogger<MyService>();
    }
}
```

### 2. 记录不同级别的日志

```csharp
// Debug级别 - 开发调试信息（Release版本中通常不记录）
_logger.LogDebug("调试信息: 变量值 = {Value}", someValue);

// Information级别 - 一般信息
_logger.LogInformation("用户 {UserId} 执行了操作 {Action}", userId, actionName);

// Warning级别 - 警告信息
_logger.LogWarning("检测到潜在问题: {Problem}", problemDescription);

// Error级别 - 错误信息（会在InfoBar中显示）
_logger.LogError("操作失败: {ErrorMessage}", errorMessage);

// Critical级别 - 严重错误（会在InfoBar中显示）
_logger.LogCritical("严重错误，应用程序可能无法继续运行");
```

### 3. 记录异常

```csharp
try
{
    // 可能抛出异常的代码
    RiskyOperation();
}
catch (Exception ex)
{
    // 记录异常和上下文信息
    _logger.LogError(ex, "执行操作失败，参数: {Parameter}", parameterValue);
    
    // 或使用高性能日志记录器
    _logger.UnexpectedException("操作执行失败", ex);
}
```

## 高性能日志记录

对于高频日志记录，建议使用预编译的高性能日志模板：

```csharp
using The_Untamed_Music_Player.Services;

// 使用预定义的高性能日志模板
_logger.SongStartedPlaying(title, artist);
_logger.DownloadProgress(title, progressPercent);
_logger.PerformanceMetric("操作名称", elapsedMs);
```

## 结构化日志

使用结构化日志可以更好地分析和查询日志：

```csharp
// 好的做法：使用命名参数
_logger.LogInformation("用户 {UserId} 在 {Timestamp} 播放了歌曲 {SongTitle}", 
    userId, DateTime.Now, songTitle);

// 避免：字符串拼接
_logger.LogInformation($"用户 {userId} 播放了歌曲 {songTitle}"); // ❌ 不推荐
```

## 性能监控日志

```csharp
public async Task PerformOperationAsync()
{
    var stopwatch = Stopwatch.StartNew();
    var operationName = "数据加载";
    
    try
    {
        _logger.LogInformation("开始执行 {OperationName}", operationName);
        
        // 执行操作
        await LoadDataAsync();
        
        stopwatch.Stop();
        _logger.PerformanceMetric(operationName, stopwatch.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.OperationFailed(operationName, ex.Message, ex);
        throw;
    }
}
```

## 条件日志记录

对于昂贵的日志信息生成，使用条件检查：

```csharp
// 避免不必要的字符串构造
if (_logger.IsEnabled(LogLevel.Debug))
{
    var expensiveDebugInfo = GenerateExpensiveDebugInfo();
    _logger.LogDebug("详细调试信息: {DebugInfo}", expensiveDebugInfo);
}
```

## 异步操作日志

```csharp
public async Task ProcessFileAsync(string filePath)
{
    var operationId = Guid.NewGuid().ToString("N")[..8];
    
    _logger.LogInformation("开始处理文件 [{OperationId}]: {FilePath}", operationId, filePath);
    
    try
    {
        await ProcessFileInternalAsync(filePath);
        _logger.LogInformation("文件处理完成 [{OperationId}]", operationId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "文件处理失败 [{OperationId}]: {FilePath}", operationId, filePath);
        throw;
    }
}
```

## 日志文件位置

- **打包应用**：`%LOCALAPPDATA%\Packages\{PackageFamily}\LocalState\Logs\`
- **非打包应用**：`%LOCALAPPDATA%\The Untamed Music Player\Logs\`

## 日志文件格式

- `app-yyyyMMdd.log`：主日志文件
- `error-yyyyMMdd.log`：错误日志文件（仅包含Error和Critical级别）

## 配置

日志配置通过 `NLog.config` 文件进行，支持：

- 日志级别控制
- 文件滚动策略
- 输出格式定制
- 异步写入配置

## 注意事项

1. **性能**：在热路径中使用高性能日志模板
2. **安全**：避免在日志中记录敏感信息（密码、密钥等）
3. **大小**：注意日志文件大小，避免记录过大的数据
4. **AOT兼容**：避免使用反射相关的日志功能

## 示例：在ViewModel中使用

```csharp
public class MusicPlayerViewModel : ObservableObject
{
    private readonly ILogger<MusicPlayerViewModel> _logger;

    public MusicPlayerViewModel()
    {
        _logger = LoggingService.CreateLogger<MusicPlayerViewModel>();
        _logger.LogInformation("MusicPlayerViewModel 已创建");
    }

    public async Task PlaySongAsync(string songPath)
    {
        try
        {
            _logger.LogInformation("开始播放歌曲: {SongPath}", songPath);
            
            // 播放逻辑
            await PlaySongInternalAsync(songPath);
            
            _logger.SongStartedPlaying(Path.GetFileNameWithoutExtension(songPath), "未知艺术家");
        }
        catch (Exception ex)
        {
            _logger.SongPlaybackError(songPath, ex.Message, ex);
            // 这个错误会自动在InfoBar中显示
            throw;
        }
    }
}
```

## 错误和InfoBar显示

Error和Critical级别的日志会自动在MainWindow的InfoBar中显示：

- **Error**：显示为错误样式，5秒后自动关闭
- **Critical**：显示为错误样式，5秒后自动关闭
- 支持并发显示，新消息会覆盖旧消息

这个系统确保重要的错误信息能够及时通知用户，同时不干扰正常的应用程序流程。