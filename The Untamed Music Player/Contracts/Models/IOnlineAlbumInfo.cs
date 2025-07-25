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

    static async Task<IBriefOnlineAlbumInfo> CreateFromSongInfoAsync(IBriefOnlineSongInfo info)
    {
        if (info is BriefCloudOnlineSongInfo briefInfo)
        {
            return await BriefCloudOnlineAlbumInfo.CreateFromSongInfoAsync(briefInfo);
        }
        else
        {
            return await BriefCloudOnlineAlbumInfo.CreateFromSongInfoAsync(
                (BriefCloudOnlineSongInfo)info
            );
        }
    }

    static IBriefOnlineAlbumInfo CreateFromArtistAlbumAsync(IOnlineArtistAlbumInfo info)
    {
        if (info is CloudOnlineArtistAlbumInfo albumInfo)
        {
            return new BriefCloudOnlineAlbumInfo
            {
                ID = albumInfo.ID,
                Name = albumInfo.Name,
                CoverPath = albumInfo.CoverPath,
                Cover = albumInfo.Cover,
            };
        }
        else
        {
            return new BriefCloudOnlineAlbumInfo
            {
                ID = info.ID,
                Name = info.Name,
                CoverPath = info.CoverPath,
                Cover = info.Cover,
            };
        }
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

    static IDetailedOnlineAlbumInfo CreateFastOnlineAlbumInfoAsync(IBriefOnlineAlbumInfo info)
    {
        if (info is BriefCloudOnlineAlbumInfo briefInfo)
        {
            return new DetailedCloudOnlineAlbumInfo
            {
                Name = briefInfo.Name,
                Cover = briefInfo.Cover,
                ArtistsStr = briefInfo.ArtistsStr,
            };
        }
        else
        {
            return new DetailedCloudOnlineAlbumInfo
            {
                Name = info.Name,
                ArtistsStr = info.ArtistsStr,
                Cover = info.Cover,
            };
        }
    }

    static async Task<IDetailedOnlineAlbumInfo> CreateDetailedOnlineAlbumInfoAsync(
        IBriefOnlineAlbumInfo info
    )
    {
        if (info is BriefCloudOnlineAlbumInfo briefInfo)
        {
            return await DetailedCloudOnlineAlbumInfo.CreateAsync(briefInfo);
        }
        else
        {
            return await DetailedCloudOnlineAlbumInfo.CreateAsync((BriefCloudOnlineAlbumInfo)info);
        }
    }
}

public interface IOnlineArtistAlbumInfo : IArtistAlbumInfoBase
{
    bool IsAvailable { get; set; }
    long ID { get; set; }
    string? CoverPath { get; set; }
}
