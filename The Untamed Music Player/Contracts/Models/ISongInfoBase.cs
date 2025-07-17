using MemoryPack;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

namespace The_Untamed_Music_Player.Contracts.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(BriefLocalSongInfo))]
[MemoryPackUnion(1, typeof(CloudBriefOnlineSongInfo))]
public partial interface IBriefSongInfoBase : ICloneable
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
    static string GetDurationStr(TimeSpan duration) =>
        duration.Hours > 0 ? $"{duration:hh\\:mm\\:ss}" : $"{duration:mm\\:ss}";

    /// <summary>
    /// 获取发行年份字符串
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    static string GetYearStr(ushort year) => year is 0 or 1970 ? "" : year.ToString();
}

public interface IDetailedSongInfoBase : IBriefSongInfoBase
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

    static async Task<IDetailedSongInfoBase> CreateDetailedSongInfoAsync(
        IBriefSongInfoBase info,
        byte sourceMode
    )
    {
        return sourceMode switch
        {
            0 => new DetailedLocalSongInfo((BriefLocalSongInfo)info),
            1 => await CloudDetailedOnlineSongInfo.CreateAsync((IBriefOnlineSongInfo)info),
            _ => await CloudDetailedOnlineSongInfo.CreateAsync((IBriefOnlineSongInfo)info),
        };
    }

    static async Task<IDetailedSongInfoBase?> CreateDetailedSongInfoAsync(IBriefSongInfoBase info)
    {
        if (info is BriefLocalSongInfo briefInfo)
        {
            return new DetailedLocalSongInfo(briefInfo);
        }
        else if (info is CloudBriefOnlineSongInfo cloudInfo)
        {
            return await CloudDetailedOnlineSongInfo.CreateAsync(cloudInfo);
        }
        else
        {
            return null;
        }
    }
}
