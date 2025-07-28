using System.Collections.ObjectModel;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;
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

    /// <summary>
    /// 根据歌曲信息获取简要艺术家信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IBriefOnlineArtistInfo> CreateFromSongInfoAsync(IBriefOnlineSongInfo info)
    {
        return info switch
        {
            BriefCloudOnlineSongInfo cloudInfo =>
                await BriefCloudOnlineArtistInfo.CreateFromSongInfoAsync(cloudInfo),
            _ => await BriefCloudOnlineArtistInfo.CreateFromSongInfoAsync(
                (BriefCloudOnlineSongInfo)info
            ),
        };
    }

    /// <summary>
    /// 根据专辑信息获取简要艺术家信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IBriefOnlineArtistInfo> CreateFromAlbumInfoAsync(IBriefOnlineAlbumInfo info)
    {
        return info switch
        {
            BriefCloudOnlineAlbumInfo cloudInfo =>
                await BriefCloudOnlineArtistInfo.CreateFromAlbumInfoAsync(cloudInfo),
            _ => await BriefCloudOnlineArtistInfo.CreateFromAlbumInfoAsync(
                (BriefCloudOnlineAlbumInfo)info
            ),
        };
    }

    /// <summary>
    /// 根据简要艺术家信息获取该艺术家的歌曲
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<List<IBriefSongInfoBase>> GetSongsByArtistAsync(IBriefOnlineArtistInfo info)
    {
        return info switch
        {
            BriefCloudOnlineArtistInfo cloudInfo =>
                await CloudArtistDetailSearchHelper.GetSongsByArtistAsync(cloudInfo),
            _ => await CloudArtistDetailSearchHelper.GetSongsByArtistAsync(
                (BriefCloudOnlineArtistInfo)info
            ),
        };
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
            totalAlbumNum == 1
                ? "ArtistInfo_Album".GetLocalized()
                : "ArtistInfo_Albums".GetLocalized();
        var songStr =
            totalSongNum == 1 ? "AlbumInfo_Song".GetLocalized() : "AlbumInfo_Songs".GetLocalized();
        return $"{totalAlbumNum} {albumStr} • {totalSongNum} {songStr}";
    }

    /// <summary>
    /// 根据简要艺术家信息获取详细艺术家信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task<IDetailedOnlineArtistInfo> SearchArtistDetailAsync(
        IBriefOnlineArtistInfo info
    )
    {
        return info switch
        {
            BriefCloudOnlineArtistInfo cloudInfo =>
                await CloudArtistDetailSearchHelper.SearchArtistDetailAsync(cloudInfo),
            _ => await CloudArtistDetailSearchHelper.SearchArtistDetailAsync(
                (BriefCloudOnlineArtistInfo)info
            ),
        };
    }

    /// <summary>
    /// 为详细艺术家补充更多信息
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    static async Task SearchMoreArtistDetailAsync(IDetailedOnlineArtistInfo info)
    {
        var task = info switch
        {
            DetailedCloudOnlineArtistInfo cloudInfo =>
                CloudArtistDetailSearchHelper.SearchMoreArtistDetailAsync(cloudInfo),
            _ => CloudArtistDetailSearchHelper.SearchMoreArtistDetailAsync(
                (DetailedCloudOnlineArtistInfo)info
            ),
        };
        await task;
    }
}
