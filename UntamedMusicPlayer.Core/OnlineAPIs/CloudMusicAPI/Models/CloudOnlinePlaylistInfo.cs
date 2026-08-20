using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlinePlaylistInfo : IBriefOnlinePlaylistInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TotalSongNumStr { get; set; } = string.Empty;
    public string? CoverPath { get; set; }

    public static BriefCloudOnlinePlaylistInfo FromDto(CloudSearchPlaylistDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BriefCloudOnlinePlaylistInfo
        {
            ID = dto.Id,
            Name = dto.Name ?? string.Empty,
            CoverPath = dto.CoverImgUrl,
            TotalSongNumStr = IBriefOnlinePlaylistInfo.GetTotalSongNumStr(dto.TrackCount),
        };
    }

}

public sealed class DetailedCloudOnlinePlaylistInfo
    : BriefCloudOnlinePlaylistInfo,
        IDetailedOnlinePlaylistInfo
{
    public string? Introduction { get; set; }
    public List<IBriefOnlineSongInfo> SongList { get; set; } = [];

    public static DetailedCloudOnlinePlaylistInfo FromBrief(
        BriefCloudOnlinePlaylistInfo briefInfo
    )
    {
        ArgumentNullException.ThrowIfNull(briefInfo);

        return new DetailedCloudOnlinePlaylistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            TotalSongNumStr = briefInfo.TotalSongNumStr,
            CoverPath = briefInfo.CoverPath,
        };
    }
}
