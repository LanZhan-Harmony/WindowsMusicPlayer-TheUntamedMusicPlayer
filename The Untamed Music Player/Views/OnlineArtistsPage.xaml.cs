using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineArtistsPage : Page
{
    public OnlineArtistsViewModel ViewModel { get; set; }

    public OnlineArtistsPage()
    {
        ViewModel = App.GetService<OnlineArtistsViewModel>();
        InitializeComponent();
    }
}
