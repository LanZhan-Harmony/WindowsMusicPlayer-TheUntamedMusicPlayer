using Microsoft.Windows.Storage;

namespace UntamedMediaPlayer.Helpers;

internal static class PathHelper
{
    internal static string GetApplicationDataRoot()
    {
        if (RuntimeHelper.IsMSIX)
        {
            return ApplicationData.GetDefault().LocalPath;
        }
        string rootPath = Path.Combine(AppContext.BaseDirectory, "AppData");
        try
        {
            Directory.CreateDirectory(rootPath);
            using FileStream fs = File.Create(
                Path.Combine(rootPath, Path.GetRandomFileName()),
                1,
                FileOptions.DeleteOnClose);
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

    internal static string? GetPackagedApplicationDataRoot() =>
        RuntimeHelper.IsMSIX ? ApplicationData.GetDefault().LocalPath : null;
}
