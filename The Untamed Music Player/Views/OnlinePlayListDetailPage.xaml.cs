using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlinePlayListDetailPage : Page
{
    public OnlinePlayListDetailViewModel ViewModel { get; }

    public OnlinePlayListDetailPage()
    {
        ViewModel = App.GetService<OnlinePlayListDetailViewModel>();
        InitializeComponent();
    }
}
