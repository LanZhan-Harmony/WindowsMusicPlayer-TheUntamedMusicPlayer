using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

namespace The_Untamed_Music_Player.Contracts.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(BriefLocalSongInfo))]
[MemoryPackUnion(1, typeof(BriefCloudOnlineSongInfo))]
public partial interface IBriefSongInfoBase : ICloneable
{
    protected static readonly string _unknownAlbum = "SongInfo_UnknownAlbum".GetLocalized();
    protected static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();

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
    static string GetYearStr(ushort year) => year is 0 or 1970 ? "" : $"{year}";

    static async Task<string?> GetCoverPathAsync(IBriefSongInfoBase info)
    {
        return info switch
        {
            BriefLocalSongInfo localInfo => localInfo.HasCover ? localInfo.Path : null,
            BriefCloudOnlineSongInfo cloudInfo => (
                await DetailedCloudOnlineSongInfo.CreateAsync(cloudInfo)
            ).CoverPath,
            _ => null,
        };
    }
}

public interface IDetailedSongInfoBase : IBriefSongInfoBase
{
    bool IsOnline { get; set; }
    string ItemType { get; set; }
    string AlbumArtistsStr { get; set; }
    string ArtistAndAlbumStr { get; set; }
    BitmapImage? Cover { get; set; }
    string BitRate { get; set; }
    string TrackStr { get; set; }
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

    /// <summary>
    /// 根据简要歌曲信息获取详细歌曲信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IDetailedSongInfoBase> CreateDetailedSongInfoAsync(IBriefSongInfoBase info)
    {
        return info switch
        {
            BriefLocalSongInfo localInfo => new DetailedLocalSongInfo(localInfo),
            BriefCloudOnlineSongInfo cloudInfo => await DetailedCloudOnlineSongInfo.CreateAsync(
                cloudInfo
            ),
            _ => await DetailedCloudOnlineSongInfo.CreateAsync((BriefCloudOnlineSongInfo)info),
        };
    }
}
