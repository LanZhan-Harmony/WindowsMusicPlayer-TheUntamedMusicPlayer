using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PropertiesDialog : ContentDialog
{
    public IDetailedMusicInfoBase Music { get; set; }

    public PropertiesDialog(IDetailedMusicInfoBase music)
    {
        Music = music;
        InitializeComponent();
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Music.Path;
        if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        else
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }
}
