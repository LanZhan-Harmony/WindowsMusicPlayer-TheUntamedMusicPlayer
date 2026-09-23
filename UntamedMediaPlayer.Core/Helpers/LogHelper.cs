using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Core.Helpers;

/// <summary>
/// Provide an entry point for logging for static classes
/// </summary>
public static class LogHelper
{
    private static ILoggingService? _loggingService;

    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    /// <remarks>
    /// If the logging service is not available,
    /// a deferred logger will be returned that will log once the logging service is available.
    /// </remarks>
    /// <typeparam name="T">The type for which to get a logger</typeparam>
    /// <returns>logger for the specified type</returns>
    public static ILogger<T> GetLogger<T>()
    {
        return _loggingService?.LoggerFactory.CreateLogger<T>() ?? new DeferredLogger<T>();
    }

    internal static void SetLoggingService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Clears the logging service.
    /// </summary>
    /// <param name="loggingService">The logging service to clear</param>
    internal static void ClearLoggingService(ILoggingService loggingService)
    {
        if (_loggingService == loggingService)
        {
            _loggingService = null;
        }
    }

    private static ILogger<T> GetCurrentLogger<T>()
    {
        return _loggingService?.LoggerFactory.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    /// <summary>
    /// A logger that defers logging until the logging service is available.
    /// </summary>
    /// <typeparam name="T">The type for which to get a logger</typeparam>
    private sealed class DeferredLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return GetCurrentLogger<T>().BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return GetCurrentLogger<T>().IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            GetCurrentLogger<T>().Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
