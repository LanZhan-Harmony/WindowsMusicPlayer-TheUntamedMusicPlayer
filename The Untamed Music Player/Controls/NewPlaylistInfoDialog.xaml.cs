using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class NewPlaylistInfoDialog : ContentDialog
{
    public PlaylistInfo? CreatedPlaylist { get; private set; }

    public NewPlaylistInfoDialog()
    {
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    private void CreateButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameTextBox.Text;
        CreatedPlaylist = Data.PlaylistLibrary.NewPlaylist(name);
    }

    private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }
}
