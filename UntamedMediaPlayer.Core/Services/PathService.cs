using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Core.Services;

public class PathService : IPathService
{
    public string RootPath { get; }
    public string SettingsPath { get; }
    public string DatabasePath { get; }

    public PathService(string rootPath)
    {
        RootPath = rootPath;
        SettingsPath = Path.Combine(RootPath, "Settings");
        DatabasePath = Path.Combine(RootPath, "Database");
        _ = EnsureDirectoryExists();
    }

    private async Task EnsureDirectoryExists()
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(SettingsPath);
            Directory.CreateDirectory(DatabasePath);
        });
    }
}
