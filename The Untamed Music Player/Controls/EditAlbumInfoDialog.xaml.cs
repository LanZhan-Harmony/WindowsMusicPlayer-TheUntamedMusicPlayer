using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Controls;

public sealed partial class EditAlbumInfoDialog : ContentDialog
{
    public LocalAlbumInfo Album { get; set; }

    public EditAlbumInfoDialog(LocalAlbumInfo album)
    {
        Album = album;
        InitializeComponent();
    }
}
