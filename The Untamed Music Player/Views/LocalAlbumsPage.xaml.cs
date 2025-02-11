using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalAlbumsPage : Page
{
    public LocalAlbumsViewModel ViewModel
    {
        get;
    }
    public LocalAlbumsPage()
    {
        ViewModel = App.GetService<LocalAlbumsViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
        if (menuButton != null)
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
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
        if (menuButton != null)
        {
            menuButton.Visibility = Visibility.Collapsed;
        }
    }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AlbumInfo albumInfo)
        {
            var grid = (Grid)((ContentControl)AlbumGridView.ContainerFromItem(e.ClickedItem)).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedAlbum = albumInfo;
            Data.ShellPage!.GetFrame().Navigate(typeof(AlbumDetailPage), null, new SuppressNavigationTransitionInfo());
        }
    }

    private async void AlbumGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedAlbum != null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedAlbum, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
            if (animation != null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(animation, Data.SelectedAlbum, "CoverBorder");
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayButton_Click(sender, e);
    }
}
