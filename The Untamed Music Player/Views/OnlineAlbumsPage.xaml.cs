using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineAlbumsPage : Page
{
    public OnlineAlbumsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlineAlbumsPage()
    {
        ViewModel = App.GetService<OnlineAlbumsViewModel>();
        InitializeComponent();
    }

    private void OnlineAlbumsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer =
            AlbumGridView.FindDescendant<ScrollViewer>()
            ?? throw new Exception("Cannot find ScrollViewer in GridView");

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (
                !Data.OnlineMusicLibrary.OnlineAlbumInfoList.HasAllLoaded
                && _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight
                    >= _scrollViewer.ExtentHeight - 50
            )
            {
                await Data.OnlineMusicLibrary.SearchMore();
                await Task.Delay(3000);
            }
        };
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
        menuButton?.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid!.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid!.FindName("PlayButton") as Button;
        var menuButton = grid!.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
        menuButton?.Visibility = Visibility.Collapsed;
    }

    private async void AlbumGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedOnlineAlbum is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedOnlineAlbum, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    Data.SelectedOnlineAlbum,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefOnlineAlbumInfo info)
        {
            var grid = (Grid)
                (
                    (ContentControl)AlbumGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedOnlineAlbum = info;
            Data.NavigatePage = "OnlineAlbumsPage";
            Data.ShellPage!.GetFrame()
                .Navigate(
                    typeof(OnlineAlbumDetailPage),
                    "OnlineAlbumsPage",
                    new SuppressNavigationTransitionInfo()
                );
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e) { }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e) { }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e) { }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e) { }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
