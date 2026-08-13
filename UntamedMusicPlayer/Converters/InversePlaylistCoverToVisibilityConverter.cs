using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Converters;

public sealed partial class InversePlaylistCoverToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var cover = value is PlaylistInfo playlist
            ? CoverManager.GetPlaylistCoverBitmap(playlist)
            : null;
        return cover is null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
