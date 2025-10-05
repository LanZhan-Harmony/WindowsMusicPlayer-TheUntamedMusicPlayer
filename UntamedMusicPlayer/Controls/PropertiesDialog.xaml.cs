using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Services;

namespace UntamedMusicPlayer.Controls;

public sealed partial class PropertiesDialog : ContentDialog, IRecipient<ThemeChangeMessage>
{
    private readonly IDetailedSongInfoBase _song;

    public PropertiesDialog(IDetailedSongInfoBase info)
    {
        StrongReferenceMessenger.Default.Register(this);
        _song = info;
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    public void Receive(ThemeChangeMessage message)
    {
        RequestedTheme = message.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        StrongReferenceMessenger.Default.Unregister<ThemeChangeMessage>(this);
    }
}
