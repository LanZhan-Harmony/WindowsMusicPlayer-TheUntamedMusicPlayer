using Microsoft.UI.Xaml.Controls;

using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 播放列表Page : Page
{
    public 播放列表ViewModel ViewModel
    {
        get;
    }

    public 播放列表Page()
    {
        ViewModel = App.GetService<播放列表ViewModel>();
        InitializeComponent();
    }
}
