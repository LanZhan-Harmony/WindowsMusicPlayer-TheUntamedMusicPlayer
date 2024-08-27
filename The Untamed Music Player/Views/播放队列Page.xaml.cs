using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 播放队列Page : Page
{
    public 播放队列ViewModel ViewModel
    {
        get;
    }

    public 播放队列Page()
    {
        ViewModel = App.GetService<播放队列ViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.Grid_PointerEntered(sender, e);
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.Grid_PointerExited(sender, e);
    }

    private void PlayButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.PlayButton_Click(sender, e);
    }
}
