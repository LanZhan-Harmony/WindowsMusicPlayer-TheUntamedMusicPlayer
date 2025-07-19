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
    string? Description { get; set; }
}
