using TagLib;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineArtistInfo : IArtistInfoBase
{
    long ID { get; set; }
}

public interface IDetailedOnlineArtistInfo : IBriefOnlineArtistInfo
{
    int TotalAlbumNum { get; set; }
    int TotalSongNum { get; set; }
    TimeSpan TotalDuration { get; set; }
    string? Introduction { get; set; }
    List<IOnlineArtistAlbumInfo> AlbumList { get; set; }

    string GetCountStr();
    string GetDurationStr();
    string GetDescriptionStr() => $"{"ArtistInfo_Artist".GetLocalized()} ";

    static async Task<byte[]> GetCoverBytes(IBriefOnlineArtistInfo info)
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

    static IDetailedOnlineArtistInfo CreateFastOnlineArtistInfoAsync(IBriefOnlineArtistInfo info)
    {
        if (info is BriefCloudOnlineArtistInfo briefInfo)
        {
            return new DetailedCloudOnlineArtistInfo { Cover = briefInfo.Cover };
        }
        else
        {
            return new DetailedCloudOnlineArtistInfo { Cover = info.Cover };
        }
    }

    static async Task<IDetailedOnlineArtistInfo> CreateDetailedOnlineArtistInfoAsync(
        IBriefOnlineArtistInfo info
    )
    {
        if (info is BriefCloudOnlineArtistInfo briefInfo)
        {
            return await DetailedCloudOnlineArtistInfo.CreateAsync(briefInfo);
        }
        else
        {
            return await DetailedCloudOnlineArtistInfo.CreateAsync(
                (BriefCloudOnlineArtistInfo)info
            );
        }
    }
}
