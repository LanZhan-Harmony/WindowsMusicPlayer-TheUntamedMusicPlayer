using Microsoft.UI.Xaml.Media.Imaging;

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
