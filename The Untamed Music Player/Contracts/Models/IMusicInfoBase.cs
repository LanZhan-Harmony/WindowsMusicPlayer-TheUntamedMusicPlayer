using MemoryPack;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

namespace The_Untamed_Music_Player.Contracts.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(BriefMusicInfo))]
[MemoryPackUnion(1, typeof(CloudBriefOnlineMusicInfo))]
public partial interface IBriefMusicInfoBase : ICloneable
{
    /// <summary>
    /// 是否可以播放
    /// </summary>
    bool IsPlayAvailable { get; set; }

    /// <summary>
    /// 在播放队列中的索引
    /// </summary>
    int PlayQueueIndex { get; set; }

    /// <summary>
    /// 路径
    /// </summary>
    string Path { get; set; }

    /// <summary>
    /// 歌曲名
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// 专辑名
    /// </summary>
    string Album { get; set; }

    /// <summary>
    /// 艺术家名字符串
    /// </summary>
    string ArtistsStr { get; set; }

    /// <summary>
    /// 时长字符串
    /// </summary>
    string DurationStr { get; set; }

    /// <summary>
    /// 发行年份字符串
    /// </summary>
    string YearStr { get; set; }

    /// <summary>
    /// 流派字符串
    /// </summary>
    string GenreStr { get; set; }

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
    static string GetYearStr(ushort year) => year is 0 or 1970 ? "" : year.ToString();

    /// <summary>
    /// 获取文本前景色
    /// </summary>
    /// <param name="currentMusic"></param>
    /// <param name="isDarkTheme"></param>
    /// <returns>如果是当前播放歌曲, 返回主题色, 如果不是, 根据当前主题返回黑色或白色</returns>
    SolidColorBrush GetTextForeground(IDetailedMusicInfoBase? currentMusic, bool isDarkTheme)
    {
        var defaultColor = isDarkTheme ? Colors.White : Colors.Black;
        var highlightColor = isDarkTheme
            ? ColorHelper.FromArgb(0xFF, 0x42, 0x9C, 0xE3)
            : ColorHelper.FromArgb(0xFF, 0x00, 0x5A, 0x9E);

        if (currentMusic is not null &&
            (currentMusic.IsOnline
            ? ((IBriefOnlineMusicInfo)this).ID == ((IDetailedOnlineMusicInfo)currentMusic).ID
            : Path == currentMusic.Path))
        {
            return new SolidColorBrush(highlightColor);
        }
        return new SolidColorBrush(defaultColor);
    }


    SolidColorBrush GetTextForeground(IDetailedMusicInfoBase? currentMusic, bool isDarkTheme, int playQueueIndex)
    {
        var defaultColor = isDarkTheme ? Colors.White : Colors.Black;
        if (currentMusic is not null &&
            (currentMusic.IsOnline
            ? ((IBriefOnlineMusicInfo)this).ID == ((IDetailedOnlineMusicInfo)currentMusic).ID
            : Path == currentMusic.Path)
            && PlayQueueIndex == playQueueIndex)
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
    bool IsOnline { get; set; }
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

    static async Task<IDetailedMusicInfoBase> CreateDetailedMusicInfoAsync(IBriefMusicInfoBase info, byte sourceMode)
    {
        return sourceMode switch
        {
            0 => new DetailedMusicInfo((BriefMusicInfo)info),
            1 => await CloudDetailedOnlineMusicInfo.CreateAsync((IBriefOnlineMusicInfo)info),
            _ => await CloudDetailedOnlineMusicInfo.CreateAsync((IBriefOnlineMusicInfo)info),
        };
    }

    static async Task<IDetailedMusicInfoBase?> CreateDetailedMusicInfoAsync(IBriefMusicInfoBase info)
    {
        if (info is BriefMusicInfo briefInfo)
        {
            return new DetailedMusicInfo(briefInfo);
        }
        else if (info is CloudBriefOnlineMusicInfo cloudInfo)
        {
            return await CloudDetailedOnlineMusicInfo.CreateAsync(cloudInfo);
        }
        else
        {
            return null;
        }
    }
}
