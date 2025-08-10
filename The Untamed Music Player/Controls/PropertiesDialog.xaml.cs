using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class PropertiesDialog : ContentDialog
{
    private readonly IDetailedSongInfoBase _song;

    public PropertiesDialog(IDetailedSongInfoBase info)
    {
        _song = info;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _song.Path;
        if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        else
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
    }
}
