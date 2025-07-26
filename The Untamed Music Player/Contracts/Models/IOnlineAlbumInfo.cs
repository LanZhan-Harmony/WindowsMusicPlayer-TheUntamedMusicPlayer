using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineAlbumInfo : IAlbumInfoBase
{
    static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();
    long ID { get; set; }

    static async Task<byte[]> GetCoverBytes(IBriefOnlineAlbumInfo info)
    {
        if (info.Cover is not null)
        {
            try
            {
                using var httpClient = new HttpClient();
                return await httpClient.GetByteArrayAsync(info.CoverPath);
            }
            catch { }
        }
        return [];
    }

    /// <summary>
    /// 根据歌曲信息获取简要专辑信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IBriefOnlineAlbumInfo> CreateFromSongInfoAsync(IBriefSongInfoBase info)
    {
        return info switch
        {
            BriefCloudOnlineSongInfo cloudInfo =>
                await BriefCloudOnlineAlbumInfo.CreateFromSongInfoAsync(cloudInfo),
            _ => await BriefCloudOnlineAlbumInfo.CreateFromSongInfoAsync(
                (BriefCloudOnlineSongInfo)info
            ),
        };
    }

    /// <summary>
    /// 根据艺术家专辑信息获取简要专辑信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static IBriefOnlineAlbumInfo CreateFromArtistAlbumAsync(IOnlineArtistAlbumInfo info)
    {
        return info switch
        {
            CloudOnlineArtistAlbumInfo cloudInfo => new BriefCloudOnlineAlbumInfo
            {
                ID = cloudInfo.ID,
                Name = cloudInfo.Name,
                CoverPath = cloudInfo.CoverPath,
                Cover = cloudInfo.Cover,
            },
            _ => new BriefCloudOnlineAlbumInfo
            {
                ID = info.ID,
                Name = info.Name,
                CoverPath = info.CoverPath,
                Cover = info.Cover,
            },
        };
    }
}

public interface IDetailedOnlineAlbumInfo : IBriefOnlineAlbumInfo
{
    int TotalNum { get; set; }
    TimeSpan TotalDuration { get; set; }
    ushort Year { get; set; }
    string DescriptionStr { get; set; }
    string? Introduction { get; set; }
    List<IBriefOnlineSongInfo> SongList { get; set; }

    static string GetDescriptionStr(ushort year, int totalNum, TimeSpan totalDuration)
    {
        var parts = new List<string>();
        if (year is not (0 or 1970))
        {
            parts.Add($"{year}");
        }
        parts.Add(
            totalNum > 1
                ? $"{totalNum} {"AlbumInfo_Songs".GetLocalized()}"
                : $"{totalNum} {"AlbumInfo_Song".GetLocalized()}"
        );
        parts.Add(
            totalDuration.Hours > 0
                ? $"{totalDuration:hh\\:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
                : $"{totalDuration:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
        );
        return string.Join(" • ", parts);
    }

    /// <summary>
    /// 根据简要专辑信息获取详细专辑信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IDetailedOnlineAlbumInfo> CreateDetailedOnlineAlbumInfoAsync(
        IBriefOnlineAlbumInfo info
    )
    {
        return info switch
        {
            BriefCloudOnlineAlbumInfo cloudInfo => await DetailedCloudOnlineAlbumInfo.CreateAsync(
                cloudInfo
            ),
            _ => await DetailedCloudOnlineAlbumInfo.CreateAsync((BriefCloudOnlineAlbumInfo)info),
        };
    }
}

public interface IOnlineArtistAlbumInfo : IArtistAlbumInfoBase
{
    bool IsAvailable { get; set; }
    long ID { get; set; }
    string? CoverPath { get; set; }
}
