using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Core.Services;

public class PathService : IPathService
{
    public string RootFolderPath { get; }
    public string SettingsFolderPath { get; }
    public string DatabaseFolderPath { get; }
    public string LogFolderPath { get; }

    public PathService(string rootFolderPath)
    {
        RootFolderPath = rootFolderPath;
        SettingsFolderPath = Path.Combine(RootFolderPath, "Settings");
        DatabaseFolderPath = Path.Combine(RootFolderPath, "Database");
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(DatabaseFolderPath);
        Directory.CreateDirectory(LogFolderPath);
    }
}
