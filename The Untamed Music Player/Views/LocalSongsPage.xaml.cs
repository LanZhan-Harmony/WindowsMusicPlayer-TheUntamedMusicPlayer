using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Controls;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalSongsPage : Page
{
    public LocalSongsViewModel ViewModel { get; }

    private ScrollViewer? _scrollViewer;

    public LocalSongsPage()
    {
        ViewModel = App.GetService<LocalSongsViewModel>();
        InitializeComponent();
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Visible;
        playButton?.Visibility = Visibility.Visible;
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        checkBox?.Visibility = Visibility.Collapsed;
        playButton?.Visibility = Visibility.Collapsed;
    }

    private void SongListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            _scrollViewer =
                listView.FindDescendant<ScrollViewer>()
                ?? throw new Exception("未找到ScrollViewer");
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void EditInfoButton_Click(object sender, RoutedEventArgs e) { }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            var song = new DetailedLocalSongInfo(info);
            var dialog = new PropertiesDialog(song) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefLocalSongInfo info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
