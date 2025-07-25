using System.Collections.ObjectModel;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineArtistInfo : IArtistInfoBase
{
    long ID { get; set; }

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

    static async Task<IBriefOnlineArtistInfo> CreateFromSongInfoAsync(IBriefOnlineSongInfo info)
    {
        if (info is BriefCloudOnlineSongInfo briefInfo)
        {
            return await BriefCloudOnlineArtistInfo.CreateFromSongInfoAsync(briefInfo);
        }
        else
        {
            return await BriefCloudOnlineArtistInfo.CreateFromSongInfoAsync(
                (BriefCloudOnlineSongInfo)info
            );
        }
    }

    static async Task<IBriefOnlineArtistInfo> CreateFromAlbumInfoAsync(IBriefOnlineAlbumInfo info)
    {
        if (info is BriefCloudOnlineAlbumInfo briefInfo)
        {
            return await BriefCloudOnlineArtistInfo.CreateFromAlbumInfoAsync(briefInfo);
        }
        else
        {
            return await BriefCloudOnlineArtistInfo.CreateFromAlbumInfoAsync(
                (BriefCloudOnlineAlbumInfo)info
            );
        }
    }
}

public interface IDetailedOnlineArtistInfo : IBriefOnlineArtistInfo
{
    bool HasAllLoaded { get; set; }
    int TotalAlbumNum { get; set; }
    int TotalSongNum { get; set; }
    TimeSpan TotalDuration { get; set; }
    string CountStr { get; set; }
    string DescriptionStr { get; set; }
    string? Introduction { get; set; }
    ObservableCollection<IOnlineArtistAlbumInfo> AlbumList { get; set; }

    void Add(IOnlineArtistAlbumInfo? info);

    static string GetCountStr(int totalAlbumNum, int totalSongNum)
    {
        var albumStr =
            totalAlbumNum > 1
                ? "ArtistInfo_Albums".GetLocalized()
                : "ArtistInfo_Album".GetLocalized();
        var songStr =
            totalSongNum > 1 ? "AlbumInfo_Songs".GetLocalized() : "AlbumInfo_Song".GetLocalized();
        return $"{totalAlbumNum} {albumStr} • {totalSongNum} {songStr}";
    }

    static IDetailedOnlineArtistInfo CreateFastOnlineArtistInfoAsync(IBriefOnlineArtistInfo info)
    {
        if (info is BriefCloudOnlineArtistInfo briefInfo)
        {
            return new DetailedCloudOnlineArtistInfo
            {
                Name = briefInfo.Name,
                Cover = briefInfo.Cover,
            };
        }
        else
        {
            return new DetailedCloudOnlineArtistInfo { Name = info.Name, Cover = info.Cover };
        }
    }
}
