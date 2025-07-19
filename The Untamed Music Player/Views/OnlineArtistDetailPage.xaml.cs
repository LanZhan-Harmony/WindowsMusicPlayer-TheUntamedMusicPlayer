using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineArtistDetailPage : Page
{
    public OnlineArtistDetailViewModel ViewModel { get; }

    public OnlineArtistDetailPage()
    {
        ViewModel = App.GetService<OnlineArtistDetailViewModel>();
        InitializeComponent();
    }
}
