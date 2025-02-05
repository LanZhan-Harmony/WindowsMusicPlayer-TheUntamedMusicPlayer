using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.UI;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LocalSongsPage : Page
{
    public LocalSongsViewModel ViewModel
    {
        get;
    }
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
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PlayButton_Click(sender, e);
    }

    private void SongListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (Data.MusicPlayer.CurrentMusic != null && sender is ListView listView)
        {
            var path = Data.MusicPlayer.CurrentMusic.Path;
            var item = Data.MusicLibrary.Songs.FirstOrDefault(x => x.Path == path);
            if (item != null)
            {
                listView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                listView.UpdateLayout();
                listView.Focus(FocusState.Programmatic);
            }
        }
    }

    public Brush GetAlternateBackgroundBrush(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            return new SolidColorBrush(Color.FromArgb(240, 48, 53, 57));
        }
        else
        {
            return new SolidColorBrush(Color.FromArgb(240, 253, 254, 254));
        }
    }
}