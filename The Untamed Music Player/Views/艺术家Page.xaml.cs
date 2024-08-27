using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 艺术家Page : Page
{
    public 艺术家ViewModel ViewModel
    {
        get;
    }
    public 艺术家Page()
    {
        ViewModel = App.GetService<艺术家ViewModel>();
        InitializeComponent();
    }
}
