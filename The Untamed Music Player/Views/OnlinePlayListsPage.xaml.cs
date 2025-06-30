using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlinePlayListsPage : Page
{
    public OnlinePlayListsViewModel ViewModel { get; set; }

    public OnlinePlayListsPage()
    {
        ViewModel = App.GetService<OnlinePlayListsViewModel>();
        InitializeComponent();
    }
}
