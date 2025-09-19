using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class NewPlaylistInfoDialog : ContentDialog, IRecipient<ThemeChangeMessage>
{
    public PlaylistInfo? CreatedPlaylist { get; private set; }

    public NewPlaylistInfoDialog()
    {
        StrongReferenceMessenger.Default.Register(this);
        RequestedTheme = ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        InitializeComponent();
    }

    public void Receive(ThemeChangeMessage message)
    {
        RequestedTheme = message.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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

    private new void CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        StrongReferenceMessenger.Default.Unregister<ThemeChangeMessage>(this);
    }
}
