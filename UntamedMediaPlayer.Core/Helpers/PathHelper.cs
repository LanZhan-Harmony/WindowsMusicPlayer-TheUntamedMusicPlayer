using Windows.Storage;

namespace UntamedMediaPlayer.Core.Helpers;

internal static class PathHelper
{
    internal static string RootFolderPath { get; }
    internal static string SettingsFolderPath { get; }
    internal static string DatabaseFolderPath { get; }
    internal static string LogFolderPath { get; }

    static PathHelper()
    {
        RootFolderPath = GetApplicationDataRoot();
        SettingsFolderPath = Path.Combine(RootFolderPath, "Settings");
        DatabaseFolderPath = Path.Combine(RootFolderPath, "Database");
        LogFolderPath = Path.Combine(RootFolderPath, "Logs");
        EnsureDirectoryExists();
    }

    private static void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(RootFolderPath);
        Directory.CreateDirectory(SettingsFolderPath);
        Directory.CreateDirectory(DatabaseFolderPath);
        Directory.CreateDirectory(LogFolderPath);
    }

    private static string GetApplicationDataRoot()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        string rootPath = Path.Combine(AppContext.BaseDirectory, "AppData");
        try
        {
            Directory.CreateDirectory(rootPath);
            using FileStream fs = File.Create(
                Path.Combine(rootPath, Path.GetRandomFileName()),
                1,
                FileOptions.DeleteOnClose
            );
            return rootPath;
        }
        catch
        {
            rootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UntamedMediaPlayer"
            );
            return rootPath;
        }
    }

    internal static string? GetPackagedApplicationDataRoot()
    {
        return RuntimeHelper.IsMSIX ? ApplicationData.Current.LocalFolder.Path : null;
    }
}
