using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalArtistsPage : Page
{
    public LocalArtistsViewModel ViewModel
    {
        get;
    }

    public LocalArtistsPage()
    {
        ViewModel = App.GetService<LocalArtistsViewModel>();
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
}
