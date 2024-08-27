using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class 歌曲Page : Page
{
    public 歌曲ViewModel ViewModel
    {
        get;
    }
    public 歌曲Page()
    {
        ViewModel = App.GetService<歌曲ViewModel>();
        InitializeComponent();
        //SongListView.ContainerContentChanging += SongListView_Loaded;
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.Grid_PointerEntered(sender, e);
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.Grid_PointerExited(sender, e);
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayButton_Click(sender, e);
    }
}