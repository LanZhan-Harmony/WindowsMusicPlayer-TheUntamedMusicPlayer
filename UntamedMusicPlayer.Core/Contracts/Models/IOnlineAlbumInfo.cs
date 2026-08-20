using UntamedMusicPlayer.Core.Helpers;

namespace UntamedMusicPlayer.Core.Contracts.Models;

public interface IBriefOnlineAlbumInfo : IAlbumInfoBase
{
    static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();
    long ID { get; set; }
    string? CoverPath { get; set; }

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
            totalNum == 1
                ? $"{totalNum} {"AlbumInfo_Song".GetLocalized()}"
                : $"{totalNum} {"AlbumInfo_Songs".GetLocalized()}"
        );
        parts.Add(
            totalDuration.Hours > 0
                ? $"{totalDuration:hh\\:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
                : $"{totalDuration:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
        );
        return string.Join(" • ", parts);
    }

}

public interface IOnlineArtistAlbumInfo : IArtistAlbumInfoBase
{
    bool IsAvailable { get; set; }
    long ID { get; set; }
}
