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
        Unloaded += LocalSongsPage_Unloaded;
    }

    private void LocalSongsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ScrollViewerVerticalOffset = _scrollViewer!.VerticalOffset;
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox is not null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton is not null)
        {
            playButton.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox is not null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton is not null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
    }

    private void SongListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            _scrollViewer =
                listView.FindDescendant<ScrollViewer>()
                ?? throw new Exception("未找到ScrollViewer");
            _scrollViewer.ChangeView(null, ViewModel.ScrollViewerVerticalOffset, null, true);
        }

        /*if (Data.MusicPlayer.CurrentMusic is not null && sender is ListView listView)
        {
            var path = Data.MusicPlayer.CurrentMusic.Path;
            var item = Data.MusicLibrary.Songs.FirstOrDefault(x => x.Path == path);
            if (item is not null)
            {
                listView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                listView.UpdateLayout();
                listView.Focus(FocusState.Programmatic);
            }
        }*/
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo info })
        {
            ViewModel.PlayButton_Click(info);
        }
    }

    private void PlayNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo info })
        {
            ViewModel.PlayNextButton_Click(info);
        }
    }

    private void EditInfoButton_Click(object sender, RoutedEventArgs e) { }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo info })
        {
            var music = new DetailedMusicInfo(info);
            var dialog = new PropertiesDialog(music) { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
        }
    }

    private void ShowAlbumButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo info })
        {
            ViewModel.ShowAlbumButton_Click(info);
        }
    }

    private void ShowArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo info })
        {
            ViewModel.ShowArtistButton_Click(info);
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e) { }
}
