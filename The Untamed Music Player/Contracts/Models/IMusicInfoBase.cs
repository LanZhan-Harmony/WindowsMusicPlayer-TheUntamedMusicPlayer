using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Contracts.Models;
public interface IBriefMusicInfoBase
{
    string Path { get; set; }
    string Title { get; set; }
    string Album { get; set; }
    string ArtistsStr { get; set; }
    string DurationStr { get; set; }
    string YearStr { get; set; }

    /// <summary>
    /// 获取参与创作的艺术家名字符串
    /// </summary>
    /// <param name="artists"></param>
    /// <returns></returns>
    static string GetArtistsStr(string[] artists) => string.Join(", ", artists);

    /// <summary>
    /// 获取时长字符串
    /// </summary>
    /// <returns></returns>
    static string GetDurationStr(TimeSpan duration) => duration.Hours > 0 ? $"{duration:hh\\:mm\\:ss}" : $"{duration:mm\\:ss}";

    /// <summary>
    /// 获取发行年份字符串
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    static string GetYearStr(ushort year) => year is 0 ? "" : year.ToString();

    /// <summary>
    /// 获取文本前景色
    /// </summary>
    /// <param name="currentMusic"></param>
    /// <param name="isDarkTheme"></param>
    /// <returns>如果是当前播放歌曲, 返回主题色, 如果不是, 根据当前主题返回黑色或白色</returns>
    SolidColorBrush GetTextForeground(IDetailedMusicInfoBase? currentMusic, bool isDarkTheme)
    {
        var defaultColor = isDarkTheme ? Colors.White : Colors.Black;

        if (currentMusic != null && Path == currentMusic.Path)
        {
            var highlightColor = isDarkTheme
                ? ColorHelper.FromArgb(0xFF, 0x42, 0x9C, 0xE3)
                : ColorHelper.FromArgb(0xFF, 0x00, 0x5A, 0x9E);
            return new SolidColorBrush(highlightColor);
        }
        return new SolidColorBrush(defaultColor);
    }
}

public interface IDetailedMusicInfoBase : IBriefMusicInfoBase
{
    bool IsPlayAvailable { get; set; }
    bool IsOnline { get; set; }
    string GenreStr { get; set; }
    string ItemType { get; set; }
    string AlbumArtistsStr { get; set; }
    string ArtistAndAlbumStr { get; set; }
    BitmapImage? Cover { get; set; }
    string BitRate { get; set; }
    string Track { get; set; }
    string Lyric { get; set; }

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <param name="albumArtists"></param>
    /// <returns></returns>
    static string GetAlbumArtistsStr(string[] albumArtists) => string.Join(", ", albumArtists);

    /// <summary>
    /// 获取艺术家和专辑名字符串
    /// </summary>
    /// <param name="album"></param>
    /// <param name="artistsStr"></param>
    /// <returns></returns>
    static string GetArtistAndAlbumStr(string album, string artistsStr)
    {
        if (string.IsNullOrEmpty(artistsStr))
        {
            return album ?? "";
        }
        if (string.IsNullOrEmpty(album))
        {
            return artistsStr;
        }
        return $"{artistsStr} • {album}";
    }
}
