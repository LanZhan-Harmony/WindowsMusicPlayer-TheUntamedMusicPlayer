using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class RenamePlaylistInfoDialog : ContentDialog
{
    private static readonly ILogger _logger =
        LoggingService.CreateLogger<RenamePlaylistInfoDialog>();
    private readonly PlaylistInfo _playlist;

    public RenamePlaylistInfoDialog(PlaylistInfo info)
    {
        _playlist = info;
        RequestedTheme = Data.MainViewModel!.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void RenameButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var newName = RenameTextBox.Text;
        var result = Data.PlaylistLibrary.RenamePlaylist(_playlist, newName);
        if (!result)
        {
            _logger.SamePlaylistName();
        }
    }

    private void RenameTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }
}
