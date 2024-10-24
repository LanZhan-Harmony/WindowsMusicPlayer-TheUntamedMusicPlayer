using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Models;
using Windows.UI;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayQueueViewModel : ObservableRecipient
{
    public PlayQueueViewModel()
    {
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

    public void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var fontIcon = grid?.FindName("MusicFontIcon") as FontIcon;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
        if (fontIcon != null)
        {
            fontIcon.Visibility = Visibility.Collapsed;
        }
    }

    public void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var fontIcon = grid?.FindName("MusicFontIcon") as FontIcon;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
        if (fontIcon != null)
        {
            fontIcon.Visibility = Visibility.Visible;
        }
    }

    public void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.ClearPlayQueue();
    }
}
