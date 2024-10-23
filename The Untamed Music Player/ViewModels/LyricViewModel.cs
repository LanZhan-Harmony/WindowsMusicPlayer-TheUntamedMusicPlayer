using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public partial class LyricViewModel : ObservableRecipient
{
    public LyricViewModel()
    {
    }

    public void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LyricSlice lyricSlice)
        {
            var time = lyricSlice.Time;
            Data.MusicPlayer.LyricProgressUpdate(time);
        }
    }
}
