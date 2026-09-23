using Microsoft.Extensions.Logging;
using UntamedMediaPlayer.Core.Models.Settings;

namespace UntamedMediaPlayer.Core.Contracts.Services;

/// <summary>
/// Application logging service
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Gets the shared logger factory for the application
    /// </summary>
    ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Binds the logging settings to the logging service
    /// </summary>
    /// <param name="settings">Developer settings</param>
    void ApplySettings(DeveloperSettings settings);
}
