using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class OnlinePlayListsPage : Page
{
    public OnlinePlayListsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;

    public OnlinePlayListsPage()
    {
        ViewModel = App.GetService<OnlinePlayListsViewModel>();
        InitializeComponent();
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IBriefOnlinePlaylistInfo info } menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = new Tuple<IBriefOnlinePlaylistInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (
            sender is MenuFlyoutItem
            {
                DataContext: Tuple<IBriefOnlinePlaylistInfo, PlaylistInfo> tuple
            }
        )
        {
            var (playlistInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButton_Click(playlistInfo, playlist);
        }
    }

    private void OnlinePlaylistsPage_Loaded(object sender, RoutedEventArgs e)
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

    private void PlaylistGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IBriefOnlinePlaylistInfo info)
        {
            var grid = (Grid)
                (
                    (ContentControl)PlaylistGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedOnlinePlaylist = info;
            Data.ShellPage!.Navigate(
                nameof(OnlinePlayListDetailPage),
                nameof(OnlinePlayListsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

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

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlinePlaylistInfo info })
        {
            ViewModel.AddToPlayQueueButton_Click(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlinePlaylistInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButton_Click(info, dialog.CreatedPlaylist);
            }
        }
    }

    private void ShowPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlinePlaylistInfo info })
        {
            var grid = (Grid)
                ((ContentControl)PlaylistGridView.ContainerFromItem(info)).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedOnlinePlaylist = info;
            Data.ShellPage!.Navigate(
                nameof(OnlinePlayListDetailPage),
                nameof(OnlinePlayListsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
