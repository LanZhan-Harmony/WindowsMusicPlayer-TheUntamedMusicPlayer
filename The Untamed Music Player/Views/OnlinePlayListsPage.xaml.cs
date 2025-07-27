using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class OnlinePlayListsPage : Page
{
    public OnlinePlayListsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlinePlayListsPage()
    {
        ViewModel = App.GetService<OnlinePlayListsViewModel>();
        InitializeComponent();
    }

    private void OnlinePlaylistsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _scrollViewer =
            PlaylistGridView.FindDescendant<ScrollViewer>()
            ?? throw new Exception("Cannot find ScrollViewer in GridView");

        _scrollViewer.ViewChanged += async (s, e) =>
        {
            if (
                !Data.OnlineMusicLibrary.OnlinePlaylistInfoList.HasAllLoaded
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

    private async void PlaylistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedOnlinePlaylist is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedOnlinePlaylist, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    Data.SelectedOnlinePlaylist,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }

    private void PlaylistGridView_ItemClick(object sender, ItemClickEventArgs e) { }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlinePlaylistInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlinePlaylistInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void ShowPlaylistButton_Click(object sender, RoutedEventArgs e) { }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
