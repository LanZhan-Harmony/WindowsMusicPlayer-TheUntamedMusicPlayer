using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Services;

namespace UntamedMusicPlayer.Converters;

public sealed partial class PlaylistCoverConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value is PlaylistInfo playlist
            ? CoverManager.GetPlaylistCoverBitmap(playlist)
            : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
