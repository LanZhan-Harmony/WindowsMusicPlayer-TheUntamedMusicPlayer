using MemoryPack;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

[MemoryPackable]
public partial class BriefCloudOnlineSongInfo : IBriefOnlineSongInfo
{
    protected static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<BriefCloudOnlineSongInfo>();

    public bool IsPlayAvailable { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long ID { get; set; }
    public virtual string Album { get; set; } = string.Empty;
    public long AlbumID { get; set; }
    public virtual string ArtistsStr { get; set; } = string.Empty;
    public long ArtistID { get; set; }
    public TimeSpan Duration { get; set; }
    public virtual string DurationStr { get; set; } = string.Empty;
    public string YearStr { get; set; } = string.Empty;
    public string GenreStr { get; set; } = string.Empty;

    [MemoryPackConstructor]
    public BriefCloudOnlineSongInfo() { }

    public BriefCloudOnlineSongInfo(CloudSearchSongDto dto, bool isAvailable)
    {
        IsPlayAvailable = isAvailable;
        if (!isAvailable)
        {
            return;
        }

        try
        {
            ID = dto.Id;
            Title = dto.Name ?? string.Empty;
            Album = dto.Album?.Name is { Length: > 0 } album
                ? album
                : IBriefSongInfoBase._unknownAlbum;
            AlbumID = dto.Album?.Id ?? 0;
            ArtistID = dto.Artists?.FirstOrDefault()?.Id ?? 0;
            var artists = dto.Artists?
                .Select(artist => artist.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Cast<string>()
                .ToArray() ?? [];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(
                artists.Length == 0 ? [IBriefSongInfoBase._unknownArtist] : artists
            );
            Duration = TimeSpan.FromMilliseconds(dto.Duration);
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            YearStr = IBriefSongInfoBase.GetYearStr(
                dto.Album?.PublishTime > 0
                    ? (ushort)DateTimeOffset.FromUnixTimeMilliseconds(dto.Album.PublishTime).Year
                    : (ushort)0
            );
        }
        catch (Exception ex)
        {
            IsPlayAvailable = false;
            _logger.ZLogInformation(ex, $"读取网易云音乐歌曲 DTO 时发生错误");
        }
    }

    public BriefCloudOnlineSongInfo(CloudSongTrackDto dto, ushort? year = null)
    {
        try
        {
            ID = dto.Id;
            Title = dto.Name ?? string.Empty;
            var album = dto.Album?.Name;
            Album = string.IsNullOrWhiteSpace(album)
                ? IBriefSongInfoBase._unknownAlbum
                : album;
            AlbumID = dto.Album?.Id ?? 0;
            ArtistID = dto.Artists?.FirstOrDefault()?.Id ?? 0;
            var artists = dto.Artists?
                .Select(artist => artist.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Cast<string>()
                .ToArray() ?? [];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(
                artists.Length == 0 ? [IBriefSongInfoBase._unknownArtist] : artists
            );
            Duration = TimeSpan.FromMilliseconds(dto.Duration);
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            var releaseYear = year
                ?? (dto.Album?.PublishTime > 0
                    ? (ushort)DateTimeOffset.FromUnixTimeMilliseconds(dto.Album.PublishTime).Year
                    : (ushort)0);
            YearStr = IBriefSongInfoBase.GetYearStr(releaseYear);
            IsPlayAvailable = true;
        }
        catch (Exception ex)
        {
            IsPlayAvailable = false;
            _logger.ZLogInformation(ex, $"读取网易云音乐歌曲 DTO 时发生错误");
        }
    }
}

public sealed class DetailedCloudOnlineSongInfo : BriefCloudOnlineSongInfo, IDetailedOnlineSongInfo
{
    public bool IsOnline { get; set; } = true;
    public string AlbumArtistsStr { get; set; } = string.Empty;
    public string ArtistAndAlbumStr { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string BitRate { get; set; } = string.Empty;
    public string TrackStr { get; set; } = string.Empty;
    public string Lyric { get; set; } = string.Empty;

    public DetailedCloudOnlineSongInfo() { }

    public static DetailedCloudOnlineSongInfo FromBrief(BriefCloudOnlineSongInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return new DetailedCloudOnlineSongInfo
        {
            IsPlayAvailable = info.IsPlayAvailable,
            Path = info.Path,
            Title = info.Title,
            Album = info.Album,
            AlbumID = info.AlbumID,
            ArtistsStr = info.ArtistsStr,
            ArtistID = info.ArtistID,
            Duration = info.Duration,
            DurationStr = info.DurationStr,
            YearStr = info.YearStr,
            GenreStr = info.GenreStr,
        };
    }

}
