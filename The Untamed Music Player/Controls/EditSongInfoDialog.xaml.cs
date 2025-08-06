using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditSongInfoDialog : ContentDialog
{
    public DetailedLocalSongInfo Song { get; set; }

    public EditSongInfoDialog(DetailedLocalSongInfo song)
    {
        Song = song;
        InitializeComponent();
    }

    private void OpenFileLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = Song.Path;
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\"",
            UseShellExecute = true,
        };
        Process.Start(startInfo);
    }
}
