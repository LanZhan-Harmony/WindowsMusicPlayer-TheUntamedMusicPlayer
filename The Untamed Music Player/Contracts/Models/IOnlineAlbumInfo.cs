using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineAlbumInfo : IAlbumInfoBase
{
    static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();
    long ID { get; set; }
}

public interface IDetailedOnlineAlbumInfo : IBriefOnlineAlbumInfo
{
    int TotalNum { get; set; }
    TimeSpan TotalDuration { get; set; }
    ushort Year { get; set; }
    string? Introduction { get; set; }
    List<IBriefOnlineSongInfo> SongList { get; set; }
    string GetDescriptionStr();

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

    static IDetailedOnlineAlbumInfo CreateFastOnlineAlbumInfoAsync(IBriefOnlineAlbumInfo info)
    {
        if (info is BriefCloudOnlineAlbumInfo briefInfo)
        {
            return new DetailedCloudOnlineAlbumInfo { Cover = briefInfo.Cover };
        }
        else
        {
            return new DetailedCloudOnlineAlbumInfo { Cover = info.Cover };
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
    long ID { get; set; }
}
