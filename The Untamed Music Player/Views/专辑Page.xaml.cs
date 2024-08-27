using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;


public sealed partial class 专辑Page : Page
{
    public 专辑ViewModel ViewModel
    {
        get;
    }
    public 专辑Page()
    {
        ViewModel = App.GetService<专辑ViewModel>();
        InitializeComponent();
    }
}
