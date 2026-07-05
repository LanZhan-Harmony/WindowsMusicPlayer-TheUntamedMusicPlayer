using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class LocalArtistsPage : Page
{
    public LocalArtistsViewModel ViewModel { get; }
    private bool _isInitialized = false;
    private LocalArtistInfo? _lastNavigatedArtist;

    public LocalArtistsPage()
    {
        ViewModel = App.GetService<LocalArtistsViewModel>();
        InitializeComponent();
    }

    private void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.SelectedIndex = ViewModel.SortMode;
        }
    }

    private async void SortByListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e
    )
    {
        if (sender is ListView listView)
        {
            await ViewModel.ChangeSortModeAsync(listView.SelectedIndex);
        }
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: LocalArtistInfo info } menuItem)
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
                    DataContext = new Tuple<LocalArtistInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<LocalArtistInfo, PlaylistInfo> tuple })
        {
            var (artistInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButtonCommand.Execute(Tuple.Create(artistInfo, playlist));
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
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
        menuButton?.Visibility = Visibility.Collapsed;
    }

    private async void ArtistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized && _lastNavigatedArtist is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(_lastNavigatedArtist, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    _lastNavigatedArtist,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
        _isInitialized = true;
    }

    private void ArtistGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LocalArtistInfo localArtistInfo)
        {
            var grid = (Grid)
                (
                    (ContentControl)ArtistGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            _lastNavigatedArtist = localArtistInfo;
            App.GetService<INavigationService>().NavigateShell(
                nameof(LocalArtistDetailPage),
                new LocalArtistNavigationArgs(localArtistInfo, nameof(LocalArtistsPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            ViewModel.PlayButtonCommand.Execute(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            ViewModel.PlayNextButtonCommand.Execute(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            ViewModel.AddToPlayQueueButtonCommand.Execute(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButtonCommand.Execute(Tuple.Create(info, dialog.CreatedPlaylist));
            }
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            var grid = (Grid)
                ((ContentControl)ArtistGridView.ContainerFromItem(info)).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            _lastNavigatedArtist = info;
            App.GetService<INavigationService>().NavigateShell(
                nameof(LocalArtistDetailPage),
                new LocalArtistNavigationArgs(info, nameof(LocalArtistsPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}






