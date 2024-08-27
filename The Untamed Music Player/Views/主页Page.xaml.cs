using Microsoft.UI.Xaml.Controls;

using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 主页Page : Page
{
    public 主页ViewModel ViewModel
    {
        get;
    }

    public 主页Page()
    {
        ViewModel = App.GetService<主页ViewModel>();
        InitializeComponent();
    }
}
