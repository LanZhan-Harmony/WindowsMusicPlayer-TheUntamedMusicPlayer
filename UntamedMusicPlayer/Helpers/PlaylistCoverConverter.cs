using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Helpers;

public partial class PlaylistCoverConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var playlist = (PlaylistInfo)value;
        return CoverManager.GetPlaylistCoverBitmap(playlist)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
