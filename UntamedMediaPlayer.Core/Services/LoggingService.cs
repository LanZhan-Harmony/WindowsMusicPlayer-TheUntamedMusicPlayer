using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Helpers;
using UntamedMediaPlayer.Core.Models.Settings;
using ZLogger;
using ZLogger.Providers;

namespace UntamedMediaPlayer.Core.Services;

/// <summary>
/// Using ZLogger to write logs to daily rolling files and apply dynamic logging settings
/// </summary>
public sealed class LoggingService : ILoggingService, IDisposable
{
    private const LogLevel DefaultMinimumLevel = LogLevel.Information;
    private const int DefaultRetentionDays = 7;

    private readonly string _logFolderPath = PathHelper.LogFolderPath;
    private DeveloperSettings? _developerSettings;
    private LogLevel _minimumLevel = DefaultMinimumLevel;
    private int _retentionDays = DefaultRetentionDays;
    private bool _disposed;

    /// <inheritdoc />
    public ILoggerFactory LoggerFactory { get; }

    public LoggingService()
    {
        // SettingsService also needs ILogger<SettingsService>. The logger must therefore
        // be usable before settings.json has been loaded; defaults are applied first and
        // SettingsService binds the persisted DeveloperSettings afterwards.
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddFilter((category, level) => IsEnabled(level));
            builder.AddZLoggerRollingFile(GetLogFilePath, RollingInterval.Month);
        });

        LogHelper.SetLoggingService(this);
    }

    /// <inheritdoc />
    public void ApplySettings(DeveloperSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_developerSettings != settings)
        {
            _developerSettings?.PropertyChanged -= OnDeveloperSettingsChanged;
            _developerSettings = settings;
            _developerSettings.PropertyChanged += OnDeveloperSettingsChanged;
        }

        UpdateSettings(settings);
        DeleteExpiredLogFiles(NormalizeRetentionDays(settings.LogRetentionDays));
    }

    private void OnDeveloperSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is null
                or nameof(DeveloperSettings.LogMinimalLevel)
                or nameof(DeveloperSettings.LogRetentionDays)
        )
        {
            UpdateSettings((sender as DeveloperSettings)!);
        }
    }

    private void UpdateSettings(DeveloperSettings settings)
    {
        LogLevel minimumLevel = NormalizeMinimumLevel(settings.LogMinimalLevel);
        int retentionDays = NormalizeRetentionDays(settings.LogRetentionDays);

        _minimumLevel = minimumLevel;
        if (_retentionDays != retentionDays)
        {
            DeleteExpiredLogFiles(retentionDays);
        }
        _retentionDays = retentionDays;
    }

    private bool IsEnabled(LogLevel level)
    {
        return level != LogLevel.None && _minimumLevel != LogLevel.None && level >= _minimumLevel;
    }

    private string GetLogFilePath(DateTimeOffset timestamp, int sequence)
    {
        string sequenceSuffix = sequence == 0 ? string.Empty : $"-{sequence}";
        string date = timestamp.ToLocalTime().ToString("yyyy-MM-dd");
        return Path.Combine(_logFolderPath, $"{date}{sequenceSuffix}.log");
    }

    private void DeleteExpiredLogFiles(int retentionDays)
    {
        try
        {
            DateTime cutoffDate = DateTime.UtcNow.Date.AddDays(-(retentionDays - 1));
            foreach (string filePath in Directory.EnumerateFiles(_logFolderPath, "*.log"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(filePath).Date < cutoffDate)
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        ex,
                        $"[LoggingService] Failed to delete expired log file：{filePath}"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            // Logging cannot report this through the same provider while it is being
            // initialized or reconfigured, so keep this fallback side-effect free.
            Debug.WriteLine(ex, "[LoggingService] Failed to clean up expired log files");
        }
    }

    private static LogLevel NormalizeMinimumLevel(LogLevel level)
    {
        return Enum.IsDefined(level) ? level : DefaultMinimumLevel;
    }

    private static int NormalizeRetentionDays(int days)
    {
        return Math.Max(1, days);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _developerSettings?.PropertyChanged -= OnDeveloperSettingsChanged;
        _developerSettings = null;
        LogHelper.ClearLoggingService(this);
        LoggerFactory.Dispose();
    }
}
