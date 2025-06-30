using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineAlbumsPage : Page
{
    public OnlineAlbumsViewModel ViewModel { get; set; }

    public OnlineAlbumsPage()
    {
        ViewModel = App.GetService<OnlineAlbumsViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        if (checkBox is not null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton is not null)
        {
            playButton.Visibility = Visibility.Visible;
        }
        if (menuButton is not null)
        {
            menuButton.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid!.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid!.FindName("PlayButton") as Button;
        var menuButton = grid!.FindName("MenuButton") as Button;
        if (checkBox is not null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton is not null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
        if (menuButton is not null)
        {
            menuButton.Visibility = Visibility.Collapsed;
        }
    }

    private void AlbumGridView_Loaded(object sender, RoutedEventArgs e) { }

    private void PlayButton_Click(object sender, RoutedEventArgs e) { }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e) { }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e) { }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e) { }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
