using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlineAlbumInfo : IBriefOnlineAlbumInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public long ArtistID { get; set; }
    public string ArtistsStr { get; set; } = string.Empty;

    public static BriefCloudOnlineAlbumInfo FromDto(CloudSearchAlbumDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var artists = dto.Artists?
            .Select(artist => artist.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToArray() ?? [];

        return new BriefCloudOnlineAlbumInfo
        {
            ID = dto.Id,
            Name = dto.Name ?? string.Empty,
            CoverPath = dto.PicUrl,
            ArtistID = dto.Artists?.FirstOrDefault()?.Id ?? 0,
            ArtistsStr = IAlbumInfoBase.GetArtistsStr(
                artists.Length == 0 ? [IBriefOnlineAlbumInfo._unknownArtist] : artists
            ),
        };
    }

    public static BriefCloudOnlineAlbumInfo FromSong(
        BriefCloudOnlineSongInfo briefInfo,
        CloudSongAlbumDto? album
    )
    {
        ArgumentNullException.ThrowIfNull(briefInfo);

        var artists = album?.Artists?
            .Select(artist => artist.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Cast<string>()
            .ToArray() ?? [];

        return new BriefCloudOnlineAlbumInfo
        {
            ID = album?.Id ?? briefInfo.AlbumID,
            Name = album?.Name ?? briefInfo.Album,
            CoverPath = album?.PicUrl,
            ArtistID = album?.Artists?.FirstOrDefault()?.Id ?? 0,
            ArtistsStr = IAlbumInfoBase.GetArtistsStr(
                artists.Length == 0 ? [IBriefOnlineAlbumInfo._unknownArtist] : artists
            ),
        };
    }

    public static BriefCloudOnlineAlbumInfo FromArtistAlbum(IOnlineArtistAlbumInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return new BriefCloudOnlineAlbumInfo
        {
            ID = info.ID,
            Name = info.Name,
            CoverPath = info.CoverPath,
        };
    }
}

public sealed class DetailedCloudOnlineAlbumInfo
    : BriefCloudOnlineAlbumInfo,
        IDetailedOnlineAlbumInfo
{
    public int TotalNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ushort Year { get; set; }
    public string DescriptionStr { get; set; } = string.Empty;
    public string? Introduction { get; set; }
    public List<IBriefOnlineSongInfo> SongList { get; set; } = [];

    public void SetArtist(string artists) => ArtistsStr = artists;

    public void AddSong(BriefCloudOnlineSongInfo song)
    {
        ArgumentNullException.ThrowIfNull(song);
        SongList.Add(song);
        TotalNum++;
        TotalDuration += song.Duration;
    }

    public void CompleteDescription() =>
        DescriptionStr = IDetailedOnlineAlbumInfo.GetDescriptionStr(
            Year,
            TotalNum,
            TotalDuration
        );

    public static DetailedCloudOnlineAlbumInfo FromBrief(BriefCloudOnlineAlbumInfo briefInfo)
    {
        ArgumentNullException.ThrowIfNull(briefInfo);

        return new DetailedCloudOnlineAlbumInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            CoverPath = briefInfo.CoverPath,
            ArtistID = briefInfo.ArtistID,
            ArtistsStr = briefInfo.ArtistsStr,
        };
    }
}

public sealed class CloudOnlineArtistAlbumInfo : IOnlineArtistAlbumInfo
{
    public bool IsAvailable { get; set; }
    public long ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public string YearStr { get; set; } = string.Empty;
    public List<IBriefSongInfoBase> SongList { get; set; } = [];

    public static CloudOnlineArtistAlbumInfo FromDto(
        CloudArtistAlbumDto dto,
        bool isDetailed
    )
    {
        ArgumentNullException.ThrowIfNull(dto);

        var year = dto.PublishTime > 0
            ? (ushort)DateTimeOffset.FromUnixTimeMilliseconds(dto.PublishTime).Year
            : (ushort)0;

        return new CloudOnlineArtistAlbumInfo
        {
            ID = dto.Id,
            Name = isDetailed ? dto.Name ?? string.Empty : string.Empty,
            CoverPath = isDetailed ? dto.PicUrl : null,
            YearStr = isDetailed ? IArtistAlbumInfoBase.GetYearStr(year) : string.Empty,
        };
    }

    public void AddSong(BriefCloudOnlineSongInfo song)
    {
        ArgumentNullException.ThrowIfNull(song);
        SongList.Add(song);
    }

    public void MarkAvailable() => IsAvailable = SongList.Count > 0;

    public void SetUnavailable() => IsAvailable = false;
}
