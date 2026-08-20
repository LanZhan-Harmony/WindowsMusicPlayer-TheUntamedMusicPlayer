using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UntamedMusicPlayer.Core.Services;

public static class CoreLoggingService
{
    private static ILoggerFactory? _loggerFactory;

    public static void Configure(ILoggerFactory loggerFactory) =>
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public static ILogger<T> CreateLogger<T>() =>
        _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;

    public static ILogger CreateLogger(string categoryName) =>
        _loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
}
