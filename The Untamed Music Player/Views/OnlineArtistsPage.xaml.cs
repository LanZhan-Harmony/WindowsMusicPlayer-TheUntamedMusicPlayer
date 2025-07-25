using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlineArtistsPage : Page
{
    public OnlineArtistsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlineArtistsPage()
    {
        ViewModel = App.GetService<OnlineArtistsViewModel>();
        InitializeComponent();
    }

    private void OnlineArtistsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer =
            ArtistGridView.FindDescendant<ScrollViewer>()
            ?? throw new Exception("Cannot find ScrollViewer in GridView");

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (
                !Data.OnlineMusicLibrary.OnlineArtistInfoList.HasAllLoaded
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

    private void PlayButton_Click(object sender, RoutedEventArgs e) { }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e) { }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e) { }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }

    private void ArtistGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefOnlineArtistInfo info)
        {
            var grid = (Grid)
                (
                    (ContentControl)ArtistGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedOnlineArtist = info;
            Data.ShellPage!.Navigate(
                nameof(OnlineArtistDetailPage),
                nameof(OnlineArtistsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private async void ArtistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedOnlineArtist is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedOnlineArtist, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    Data.SelectedOnlineArtist,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }
}
