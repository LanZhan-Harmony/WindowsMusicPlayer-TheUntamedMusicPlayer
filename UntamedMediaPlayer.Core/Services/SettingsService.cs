using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Helpers;
using UntamedMediaPlayer.Core.Models.Settings;
using ZLogger;

namespace UntamedMediaPlayer.Core.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _folderPath = PathHelper.SettingsFolderPath;
    private readonly string _filePath;
    private readonly ILoggingService? _loggingService;

    private readonly Debouncer _writeSettingsDebouncer;

    public AppSettings Settings
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            UnsubscribeFromSettings(value);
            field = value;
            SubscribeToSettings(value);
            _loggingService?.ApplySettings(value.DeveloperSettings);
        }
    }

    public SettingsService(ILogger<SettingsService> logger, ILoggingService? loggingService = null)
    {
        _logger = logger;
        _loggingService = loggingService;
        _filePath = Path.Combine(_folderPath, "settings.json");
        _writeSettingsDebouncer = new Debouncer(
            TimeSpan.FromSeconds(1000),
            ex => _logger.ZLogError(ex, $"[SettingsService] Failed to save settings to disk")
        );
        Settings = LoadFromDisk();
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.ZLogInformation(
                    $"[SettingsService] Settings file does not exist, using default values"
                );
                return new AppSettings();
            }

            string json = File.ReadAllText(_filePath);
            AppSettings? loaded = JsonAotSerializer.Deserialize<AppSettings>(json);
            return loaded ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            _logger.ZLogError(
                ex,
                $"[SettingsService] JSON parsing failed, file backed up and default values used"
            );
            BackupCorruptedFile();
            return new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"[SettingsService] Failed to load settings, using default values"
            );
            return new AppSettings();
        }
    }

    private async Task SaveToDisk()
    {
        await _writeSettingsDebouncer.RunAsync(async () =>
        {
            string json = JsonAotSerializer.Serialize(Settings);
            string tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json);
            File.Move(tmpPath, _filePath, true);
        });
    }

    private void BackupCorruptedFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string backup = _filePath + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(_filePath, backup);
                _logger.ZLogInformation(
                    $"[SettingsService] Corrupted file has been backed up to: {backup}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"[SettingsService] Failed to backup corrupted settings file");
        }
    }

    private void SubscribeToSettings(AppSettings settings)
    {
        settings.LibrarySettings.PropertyChanged += OnSettingsChanged;
        settings.MusicPlaybackSettings.PropertyChanged += OnSettingsChanged;
        settings.VideoPlaybackSettings.PropertyChanged += OnSettingsChanged;
        settings.LyricSettings.PropertyChanged += OnSettingsChanged;
        settings.PersonalizationSettings.PropertyChanged += OnSettingsChanged;
        settings.PrivacySettings.PropertyChanged += OnSettingsChanged;
        settings.DeveloperSettings.PropertyChanged += OnSettingsChanged;
    }

    private void UnsubscribeFromSettings(AppSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        settings.LibrarySettings.PropertyChanged -= OnSettingsChanged;
        settings.MusicPlaybackSettings.PropertyChanged -= OnSettingsChanged;
        settings.VideoPlaybackSettings.PropertyChanged -= OnSettingsChanged;
        settings.LyricSettings.PropertyChanged -= OnSettingsChanged;
        settings.PersonalizationSettings.PropertyChanged -= OnSettingsChanged;
        settings.PrivacySettings.PropertyChanged -= OnSettingsChanged;
        settings.DeveloperSettings.PropertyChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = SaveToDisk();
    }

    public async Task ExportSettings(string filePath)
    {
        try
        {
            string json = JsonAotSerializer.Serialize(Settings);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"[SettingsService] Failed to export settings to {filePath}");
            throw;
        }
    }

    public async Task ImportSettings(string filePath)
    {
        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            AppSettings? imported = JsonAotSerializer.Deserialize<AppSettings>(json);
            Settings =
                imported ?? throw new InvalidOperationException("Imported settings are null.");
            _ = SaveToDisk();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"[SettingsService] Failed to import settings from {filePath}");
            throw;
        }
    }
}
