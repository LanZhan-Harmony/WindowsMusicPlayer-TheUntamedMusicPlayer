using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;

namespace UntamedMusicPlayer.Views;

public sealed partial class PlayListsPage : Page
{
    public PlayListsViewModel ViewModel { get; }

    public PlayListsPage()
    {
        ViewModel = App.GetService<PlayListsViewModel>();
        InitializeComponent();
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem { DataContext: PlaylistInfo info } menuItem)
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
                    DataContext = new Tuple<PlaylistInfo, PlaylistInfo>(info, playlist),
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: Tuple<PlaylistInfo, PlaylistInfo> tuple })
        {
            var (artistInfo, playlist) = tuple;
            ViewModel.AddToPlaylistButton_Click(artistInfo, playlist);
        }
    }

    private async void PlaylistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedPlaylist is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedPlaylist, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    Data.SelectedPlaylist,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
    }

    private void PlaylistGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is PlaylistInfo info)
        {
            var grid = (Grid)
                (
                    (ContentControl)PlaylistGridView.ContainerFromItem(e.ClickedItem)
                ).ContentTemplateRoot;
            var border = (Border)grid.Children[1];
            ConnectedAnimationService
                .GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", border);
            Data.SelectedPlaylist = info;
            Data.ShellPage!.Navigate(
                nameof(PlayListDetailPage),
                nameof(PlayListsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
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

    private void PlayLists_CreatePlaylist1(object sender, RoutedEventArgs e)
    {
        var playlistName = PlaylistNameTextBox1.Text;
        Data.PlaylistLibrary.NewPlaylist(playlistName);
        CreatePlaylistButton.Flyout.Hide();
    }

    private void PlaylistNameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as TextBox)!.Text = "";
        (sender as TextBox)!.SelectedText = "PlaylistInfo_UntitledPlaylist".GetLocalized();
    }

    private void PlaylistNameTextBox1_TextChanged(object sender, TextChangedEventArgs e)
    {
        CreatePlaylistButton.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
        CreatePlaylistFlyoutButton1.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }

    private void CreatePlaylistFlyout_Closed(object sender, object e)
    {
        CreatePlaylistButton.IsEnabled = true;
        CreatePlaylistFlyoutButton1.IsEnabled = true;
    }

    private void PlayLists_CreatePlaylist2(object sender, RoutedEventArgs e)
    {
        var playlistName = PlaylistNameTextBox2.Text;
        Data.PlaylistLibrary.NewPlaylist(playlistName);
        NewPlaylistButton.Flyout.Hide();
    }

    private void PlaylistNameTextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
        CreatePlaylistFlyoutButton2.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }

    private void NewPlaylistFlyout_Closed(object sender, object e)
    {
        CreatePlaylistFlyoutButton2.IsEnabled = true;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void AddToPlayQueueButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            ViewModel.AddToPlayQueueButton_Click(info);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
            {
                ViewModel.AddToPlaylistButton_Click(info, dialog.CreatedPlaylist);
            }
        }
    }

    private async void EditInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            var dialog = new EditPlaylistInfoDialog(info) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            var titleTextBlock = new TextBlock
            {
                Text = "PlayLists_DeleteDialogTitle".GetLocalized(),
                FontWeight = FontWeights.Normal,
            };
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                RequestedTheme = ThemeSelectorService.IsDarkTheme
                    ? ElementTheme.Dark
                    : ElementTheme.Light,
                Title = titleTextBlock,
                Content = "PlayLists_DeleteDialogContent".GetLocalizedWithReplace(
                    "{title}",
                    info.Name
                ),
                PrimaryButtonText = "PlayLists_DeleteDialogPrimary".GetLocalized(),
                CloseButtonText = "PlayLists_DeleteDialogClose".GetLocalized(),
                DefaultButton = ContentDialogButton.Close,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Data.PlaylistLibrary.DeletePlaylist(info);
            }
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
