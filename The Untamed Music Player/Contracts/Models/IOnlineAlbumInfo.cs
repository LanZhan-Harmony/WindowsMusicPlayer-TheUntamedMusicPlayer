using The_Untamed_Music_Player.Helpers;

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
    string? Description { get; set; }
}
