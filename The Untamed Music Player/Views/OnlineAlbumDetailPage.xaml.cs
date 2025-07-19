using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineAlbumDetailPage : Page
{
    public OnlineAlbumDetailViewModel ViewModel { get; }

    public OnlineAlbumDetailPage()
    {
        ViewModel = App.GetService<OnlineAlbumDetailViewModel>();
        InitializeComponent();
    }
}
