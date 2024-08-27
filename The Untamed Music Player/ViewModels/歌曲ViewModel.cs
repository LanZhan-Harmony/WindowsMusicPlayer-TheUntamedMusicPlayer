using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class 歌曲ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public 歌曲ViewModel()
    {
        Data.歌曲ViewModel = this;
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (Data.MusicPlayer.PlayQueueName != "Songs:All" || Data.MusicPlayer.PlayQueue.Count != Data.MusicLibrary.Musics.Count)
        {
            Data.MusicPlayer.SetPlayList("Songs:All", Data.MusicLibrary.Musics);
        }
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (Data.MusicPlayer.PlayQueueName != "Songs:All" || Data.MusicPlayer.PlayQueue.Count != Data.MusicLibrary.Musics.Count)
        {
            Data.MusicPlayer.SetPlayList("Songs:All", Data.MusicLibrary.Musics);
        }
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
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

    public void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
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
}
