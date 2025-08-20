using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class RenamePlaylistInfoDialog : ContentDialog
{
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
        Data.PlaylistLibrary.RenamePlaylist(_playlist, newName);
    }

    private void RenameTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }
}
