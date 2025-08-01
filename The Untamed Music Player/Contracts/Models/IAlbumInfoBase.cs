using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IAlbumInfoBase
{
    string Name { get; set; }
    BitmapImage? Cover { get; set; }
    string? CoverPath { get; set; }
    string ArtistsStr { get; set; }

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    static string GetArtistsStr(string[] artists) => string.Join(", ", artists);
}

public interface IArtistAlbumInfoBase
{
    protected static readonly string _unknownYear = "AlbumInfo_UnknownYear".GetLocalized();

    string Name { get; set; }
    string YearStr { get; set; }
    BitmapImage? Cover { get; set; }
    List<IBriefSongInfoBase> SongList { get; set; }

    static string GetYearStr(ushort year) => year is 0 or 1970 ? _unknownYear : $"{year}";
}
