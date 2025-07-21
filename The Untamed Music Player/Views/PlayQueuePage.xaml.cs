using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class PlayQueuePage : Page
{
    public PlayQueueViewModel ViewModel { get; }

    public PlayQueuePage()
    {
        ViewModel = App.GetService<PlayQueueViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var fontIcon = grid?.FindName("MusicFontIcon") as FontIcon;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
        fontIcon?.Visibility = Visibility.Collapsed;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var fontIcon = grid?.FindName("MusicFontIcon") as FontIcon;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
        fontIcon?.Visibility = Visibility.Visible;
    }

    private void PlayButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.RemoveButton_Click(info);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.MoveUpButton_Click(info);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.MoveDownButton_Click(info);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            var song = await IDetailedSongInfoBase.CreateDetailedSongInfoAsync(info);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IBriefSongInfoBase info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
