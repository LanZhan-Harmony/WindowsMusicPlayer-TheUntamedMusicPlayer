using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class PropertiesDialog : ContentDialog
{
    public IDetailedSongInfoBase Song { get; set; }

    public PropertiesDialog(IDetailedSongInfoBase song)
    {
        Song = song;
        InitializeComponent();
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Song.Path;
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
