using UntamedMediaPlayer.Core.Models;

namespace UntamedMediaPlayer.Core.Contracts.Services;

public interface ISettingsService
{
    AppSettings Settings { get; set; }

    Task ImportSettings(string filePath);
    Task ExportSettings(string filePath);
}
