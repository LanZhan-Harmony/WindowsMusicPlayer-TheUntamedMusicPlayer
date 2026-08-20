using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class LocalAlbumsPage : Page
{
    public LocalAlbumsViewModel ViewModel { get; }
    private bool _isInitialized = false;
    private LocalAlbumInfo? _lastNavigatedAlbum;

    public LocalAlbumsPage()
    {
        ViewModel = App.GetService<LocalAlbumsViewModel>();
        InitializeComponent();
    }

    public object GetAlbumGridViewSource(
        ICollectionView grouped,
        List<LocalAlbumInfo> notGrouped,
        bool isGrouped
    ) => isGrouped ? grouped : notGrouped;

    public Visibility GetAlbumGridViewVisibility(bool isActive) =>
        isActive ? Visibility.Collapsed : Visibility.Visible;

    private void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.SelectedIndex = ViewModel.SortMode;
        }
    }

    private async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            await ViewModel.ChangeSortModeAsync(listView.SelectedIndex);
        }
    }

    private void GenreListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.SelectedIndex = ViewModel.GenreMode;
        }
    }

    private async void GenreListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            await ViewModel.ChangeGenreModeAsync(listView.SelectedIndex);
        }
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: LocalAlbumInfo info } menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in App.GetService<PlaylistLibrary>().Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = new Tuple<LocalAlbumInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<LocalAlbumInfo, PlaylistInfo> tuple })
        {
            var (albumInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButtonCommand.Execute(Tuple.Create(albumInfo, playlist));
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
        if (!_isInitialized && _lastNavigatedAlbum is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(_lastNavigatedAlbum, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    _lastNavigatedAlbum,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
        _isInitialized = true;
    }

    private void AlbumGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LocalAlbumInfo info)
        {
            var grid = (Grid)
                (
                    (ContentControl)AlbumGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            _lastNavigatedAlbum = info;
            App.GetService<INavigationService>()
                .NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(info, nameof(LocalAlbumsPage)),
                    NavigationTransition.Suppress
                );
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            ViewModel.PlayButtonCommand.Execute(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            ViewModel.PlayNextButtonCommand.Execute(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            ViewModel.AddToPlayQueueButtonCommand.Execute(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButtonCommand.Execute(
                    Tuple.Create(info, dialog.CreatedPlaylist)
                );
            }
        }
    }

    private async void EditInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            var dialog = new EditAlbumInfoDialog(info) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            var grid = (Grid)
                ((ContentControl)AlbumGridView.ContainerFromItem(info)).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            _lastNavigatedAlbum = info;
            App.GetService<INavigationService>()
                .NavigateShell(
                    nameof(LocalAlbumDetailPage),
                    new LocalAlbumNavigationArgs(info, nameof(LocalAlbumsPage)),
                    NavigationTransition.Suppress
                );
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalAlbumInfo info })
        {
            ViewModel.ShowArtistButtonCommand.Execute(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
