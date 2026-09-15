using System.Diagnostics;
using System.Text.Json;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Helpers;
using UntamedMediaPlayer.Core.Models;

namespace UntamedMediaPlayer.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly IPathService _pathService;
    private readonly string _folderPath;
    private readonly string _filePath;

    private readonly Debouncer _writeSettingsDebouncer = new(
        TimeSpan.FromSeconds(1000),
        onError: ex => Debug.WriteLine($"[SettingsService] 写入设置失败：{ex.Message}")
    );

    public AppSettings Settings { get; set; }

    public SettingsService(IPathService pathService)
    {
        _pathService = pathService;
        _folderPath = _pathService.SettingsPath;
        _filePath = Path.Combine(_folderPath, "settings.json");
        Settings = LoadFromDisk();
        Settings.PropertyChanged += (s, e) => _ = SaveToDisk();
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                Debug.WriteLine("[SettingsService] 设置文件不存在，使用默认值。");
                return new AppSettings();
            }

            string json = File.ReadAllText(_filePath);
            AppSettings? loaded = JsonAotSerializer.Deserialize<AppSettings>(json);
            return loaded ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[SettingsService] JSON 解析失败：{ex.Message}，已备份并回退默认值。");
            BackupCorruptedFile();
            return new AppSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsService] 加载失败：{ex.Message}，使用默认值。");
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
            File.Move(tmpPath, _filePath, overwrite: true);
        });
    }

    private void BackupCorruptedFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var backup = _filePath + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(_filePath, backup);
                Debug.WriteLine($"[SettingsService] 损坏文件已备份到：{backup}");
            }
        }
        catch
        { /* 备份失败不影响主流程 */
        }
    }

    public Task ExportSettings(string filePath) => throw new NotImplementedException();

    public Task ImportSettings(string filePath) => throw new NotImplementedException();
}
