using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Helpers;

public partial class AlbumCoverConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var album = (LocalAlbumInfo)value;
        return CoverManager.GetAlbumCoverBitmap(album)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
