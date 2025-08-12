using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalArtistsPage : Page, IRecipient<HaveMusicMessage>
{
    public LocalArtistsViewModel ViewModel { get; }

    public LocalArtistsPage()
    {
        ViewModel = App.GetService<LocalArtistsViewModel>();
        StrongReferenceMessenger.Default.Register(this);
        InitializeComponent();
    }

    private void LocalArtistsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.Unregister<HaveMusicMessage>(this);
    }

    public void Receive(HaveMusicMessage message)
    {
        DispatcherQueue.TryEnqueue(ViewModel.LoadModeAndArtistList);
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
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
        menuButton?.Visibility = Visibility.Collapsed;
    }

    private async void ArtistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.SelectedLocalArtist is not null && sender is GridView gridView)
        {
            gridView.ScrollIntoView(Data.SelectedLocalArtist, ScrollIntoViewAlignment.Leading);
            gridView.UpdateLayout();
            var animation = ConnectedAnimationService
                .GetForCurrentView()
                .GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                await gridView.TryStartConnectedAnimationAsync(
                    animation,
                    Data.SelectedLocalArtist,
                    "CoverBorder"
                );
            }
            gridView.Focus(FocusState.Programmatic);
        }
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
            Data.SelectedLocalArtist = localArtistInfo;
            Data.ShellPage!.Navigate(
                nameof(LocalArtistDetailPage),
                nameof(LocalArtistsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LocalArtistInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
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
            Data.SelectedLocalArtist = info;
            Data.ShellPage!.Navigate(
                nameof(LocalArtistDetailPage),
                nameof(LocalArtistsPage),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
