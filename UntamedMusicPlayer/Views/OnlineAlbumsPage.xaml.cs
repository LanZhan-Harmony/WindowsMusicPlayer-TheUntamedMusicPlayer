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

public sealed partial class OnlineAlbumsPage : Page
{
    public OnlineAlbumsViewModel ViewModel { get; set; }
    private ScrollViewer? _scrollViewer;
    private bool _isSearching;

    public OnlineAlbumsPage()
    {
        ViewModel = App.GetService<OnlineAlbumsViewModel>();
        InitializeComponent();
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: IBriefOnlineAlbumInfo info } menuItem)
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
                    DataContext = new Tuple<IBriefOnlineAlbumInfo, PlaylistInfo>(info, playlist),
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
                DataContext: Tuple<IBriefOnlineAlbumInfo, PlaylistInfo> tuple
            }
        )
        {
            var (albumInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButton_Click(albumInfo, playlist);
        }
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        // checkBox?.Visibility = Visibility.Visible;
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
        var gridView = (sender as GridView)!;
        if (gridView.Visibility == Visibility.Collapsed)
        {
            return;
        }

        _scrollViewer = gridView.FindDescendant<ScrollViewer>()!;
        _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

        if (Data.SelectedOnlineAlbum is not null)
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

    private async void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (
            !_isSearching
            && !Data.OnlineMusicLibrary.OnlineAlbumInfoList.HasAllLoaded
            && _scrollViewer!.VerticalOffset + _scrollViewer.ViewportHeight
                >= _scrollViewer.ExtentHeight - 50
        )
        {
            _isSearching = true;
            await Data.OnlineMusicLibrary.SearchMore();
            _isSearching = false;
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
            Data.ShellPage!.Navigate(
                nameof(OnlineAlbumDetailPage),
                nameof(OnlineAlbumsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            ViewModel.AddToPlayQueueButton_Click(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButton_Click(info, dialog.CreatedPlaylist);
            }
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            var grid = (Grid)
                ((ContentControl)AlbumGridView.ContainerFromItem(info)).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedOnlineAlbum = info;
            Data.ShellPage!.Navigate(
                nameof(OnlineAlbumDetailPage),
                nameof(OnlineAlbumsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefOnlineAlbumInfo info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }

    private void OnlineAlbumsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer?.ViewChanged -= ScrollViewer_ViewChanged;
    }
}
