using System.Collections.ObjectModel;
using UntamedMusicPlayer.Core.Helpers;

namespace UntamedMusicPlayer.Core.Contracts.Models;

public interface IBriefOnlineArtistInfo : IArtistInfoBase
{
    long ID { get; set; }

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

}
