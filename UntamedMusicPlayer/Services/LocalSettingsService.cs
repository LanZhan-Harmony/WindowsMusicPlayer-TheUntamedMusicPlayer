using System.Text;
using Microsoft.Extensions.Options;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using Windows.Storage;

namespace UntamedMusicPlayer.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "UntamedMusicPlayer/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";

    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData
    );
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IOptions<LocalSettingsOptions> options)
    {
        _options = options.Value;

        _applicationDataFolder = Path.Combine(
            _localApplicationData,
            _options.ApplicationDataFolder ?? _defaultApplicationDataFolder
        );
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            var path = Path.Combine(_applicationDataFolder, _localsettingsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _settings =
                    await Json.ToObjectAsync<IDictionary<string, object>>(json)
                    ?? new Dictionary<string, object>();
            }

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }
        else
        {
            await InitializeAsync();

            if (_settings is not null && _settings.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T? value)
    {
        if (value is null)
        {
            return;
        }
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value);
        }
        else
        {
            await InitializeAsync();

            _settings[key] = await Json.StringifyAsync(value);

            await Task.Run(async () =>
            {
                if (!Directory.Exists(_applicationDataFolder))
                {
                    Directory.CreateDirectory(_applicationDataFolder);
                }

                var fileContent = await Json.StringifyAsync(_settings);
                File.WriteAllText(
                    Path.Combine(_applicationDataFolder, _localsettingsFile),
                    fileContent,
                    Encoding.UTF8
                );
            });
        }
    }
}
