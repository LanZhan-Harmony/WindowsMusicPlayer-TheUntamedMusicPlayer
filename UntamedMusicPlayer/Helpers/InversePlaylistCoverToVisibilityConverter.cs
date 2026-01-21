using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Helpers;

public partial class InversePlaylistCoverToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var playlist = (PlaylistInfo)value;
        var cover = CoverManager.GetPlaylistCoverBitmap(playlist);
        return cover is null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
