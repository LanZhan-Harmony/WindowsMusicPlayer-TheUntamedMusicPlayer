using UntamedMediaPlayer.Core.Helpers;

namespace UntamedMediaPlayer.Core.Compatibility;

public static class DeprecatedPathHelper
{
    public static bool HasDeprecatedAppData { get; }
    public static string? DeprecatedRootFolderPath { get; }
    public static string? SettingsFilePath { get; }
    public static string? PlaylistFolderPath { get; }
    public static string? PlaylistM3u8FolderPath { get; }
    public static string? MusicFoldersFilePath { get; }

    static DeprecatedPathHelper()
    {
        string? packagedRootPath = PathHelper.GetPackagedApplicationDataRoot();
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
