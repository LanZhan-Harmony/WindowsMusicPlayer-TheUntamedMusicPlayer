using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayListsPage : Page
{
    public PlayListsViewModel ViewModel { get; }

    public PlayListsPage()
    {
        ViewModel = App.GetService<PlayListsViewModel>();
        InitializeComponent();
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
    }

    private void PlaylistNameTextBox1_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        CreatePlaylistButton.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
        CreatePlaylistFlyoutButton1.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
    }

    private void PlayLists_CreatePlaylist2(object sender, RoutedEventArgs e)
    {
        var playlistName = PlaylistNameTextBox2.Text;
        Data.PlaylistLibrary.NewPlaylist(playlistName);
    }

    private void PlaylistNameTextBox2_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        CreatePlaylistFlyoutButton2.IsEnabled = !string.IsNullOrEmpty((sender as TextBox)!.Text);
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

    private async void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PlaylistInfo info })
        {
            var dialog = new RenamePlaylistInfoDialog(info) { XamlRoot = XamlRoot };
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
                RequestedTheme = Data.MainViewModel!.IsDarkTheme
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
