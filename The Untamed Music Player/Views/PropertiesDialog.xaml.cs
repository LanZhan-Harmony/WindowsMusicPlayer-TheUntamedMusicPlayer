using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PropertiesDialog : ContentDialog
{
    public IDetailedMusicInfoBase Music { get; set; } = Data.MusicPlayer.CurrentMusic!;

    public PropertiesDialog()
    {
        InitializeComponent();
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Music.Path;
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}
