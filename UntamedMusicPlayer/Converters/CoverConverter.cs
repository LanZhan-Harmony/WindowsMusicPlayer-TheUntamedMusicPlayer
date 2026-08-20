using Microsoft.UI.Xaml.Data;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Services;

namespace UntamedMusicPlayer.Converters;

/// <summary>
/// Converts Core cover paths or song models into presentation-layer images.
/// </summary>
public sealed partial class CoverConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            string path => CoverManager.GetCoverBitmap(path),
            LocalAlbumInfo album => CoverManager.GetAlbumCoverBitmap(album),
            LocalArtistInfo artist => CoverManager.GetEmbeddedCoverBitmap(artist.CoverPath),
            LocalArtistAlbumInfo artistAlbum => CoverManager.GetEmbeddedCoverBitmap(
                artistAlbum.CoverPath
            ),
            IDetailedSongInfoBase song => CoverManager.GetCoverBitmap(song),
            _ => null,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
