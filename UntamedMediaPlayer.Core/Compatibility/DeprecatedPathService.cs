using UntamedMediaPlayer.Core.Contracts.Compatibility;

namespace UntamedMediaPlayer.Core.Compatibility;

public sealed class DeprecatedPathService : IDeprecatedPathService
{
    public bool HasDeprecatedAppData { get; }
    public string? DeprecatedRootFolderPath { get; }
    public string? SettingsFilePath { get; }
    public string? PlaylistFolderPath { get; }
    public string? PlaylistM3u8FolderPath { get; }
    public string? MusicFoldersFilePath { get; }

    public DeprecatedPathService(string? packagedRootPath)
    {
        if (packagedRootPath is null)
        {
            HasDeprecatedAppData = false;
            return;
        }
        string? packagesFolderPath = Directory.GetParent(packagedRootPath)?.Parent?.FullName;
        if (packagesFolderPath is null)
        {
            HasDeprecatedAppData = false;
            return;
        }
        DeprecatedRootFolderPath = Path.Combine(packagesFolderPath, "C19A3696.28439CB846862_wk3cnjrz7y37w");
        if (!Directory.Exists(DeprecatedRootFolderPath))
        {
            HasDeprecatedAppData = false;
            return;
        }
        SettingsFilePath = Path.Combine(DeprecatedRootFolderPath, "Settings", "settings.dat");
        PlaylistFolderPath = Path.Combine(DeprecatedRootFolderPath, "LocalState", "PlaylistData");
        PlaylistM3u8FolderPath = Path.Combine(DeprecatedRootFolderPath, "LocalState", "PlaylistM3u8Data");
        MusicFoldersFilePath = Path.Combine(DeprecatedRootFolderPath, "LocalState", "MusicFolders.json");
        HasDeprecatedAppData = true;
    }
}
