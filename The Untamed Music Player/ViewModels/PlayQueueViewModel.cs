using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public partial class PlayQueueViewModel : ObservableRecipient
{
    public PlayQueueViewModel()
    {
    }

    public void PlayQueueListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: BriefMusicInfo briefMusicInfo })
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.ClearPlayQueue();
    }
}
