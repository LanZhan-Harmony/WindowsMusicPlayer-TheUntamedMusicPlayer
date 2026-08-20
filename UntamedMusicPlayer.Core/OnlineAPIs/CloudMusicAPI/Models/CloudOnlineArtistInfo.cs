using System.Collections.ObjectModel;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlineArtistInfo : IBriefOnlineArtistInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverPath { get; set; }

    public static BriefCloudOnlineArtistInfo FromDto(CloudSearchArtistDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BriefCloudOnlineArtistInfo
        {
            ID = dto.Id,
            Name = dto.Name ?? string.Empty,
            CoverPath = dto.PicUrl,
        };
    }

    public static BriefCloudOnlineArtistInfo FromDto(long artistId, CloudArtistDto? dto)
    {
        return new BriefCloudOnlineArtistInfo
        {
            ID = artistId,
            Name = dto?.Name ?? string.Empty,
            CoverPath = dto?.PicUrl,
        };
    }
}

public sealed class DetailedCloudOnlineArtistInfo
    : BriefCloudOnlineArtistInfo,
        IDetailedOnlineArtistInfo
{
    private readonly HashSet<long> _artistAlbumIds = [];

    public const byte Limit = 20;
    public ushort Page { get; set; }
    public int CurrentAlbumNum { get; set; }
    public bool HasAllLoaded { get; set; }
    public int TotalAlbumNum { get; set; }
    public int TotalSongNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public string CountStr { get; set; } = string.Empty;
    public string DescriptionStr { get; set; } = $"{"ArtistInfo_Artist".GetLocalized()} ";
    public string? Introduction { get; set; }
    public ObservableCollection<IOnlineArtistAlbumInfo> AlbumList { get; set; } = [];

    public void Add(IOnlineArtistAlbumInfo? info)
    {
        CurrentAlbumNum++;
        if (info is not null && info.IsAvailable && _artistAlbumIds.Add(info.ID))
        {
            AlbumList.Add(info);
        }
        if (TotalAlbumNum == CurrentAlbumNum)
        {
            HasAllLoaded = true;
        }
    }
}
