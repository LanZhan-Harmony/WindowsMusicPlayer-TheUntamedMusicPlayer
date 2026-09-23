using AppSettings = UntamedMediaPlayer.Core.Models.Settings.AppSettings;

namespace UntamedMediaPlayer.Core.Contracts.Services;

public interface ISettingsService
{
    AppSettings Settings { get; set; }

    Task ImportSettings(string filePath);
    Task ExportSettings(string filePath);
}
