namespace The_Untamed_Music_Player.Contracts.Models;
public interface IBriefOnlineMusicInfo : IBriefMusicInfoBase
{
    bool IsAvailable { get; set; }
    long ID { get; set; }
    long AlbumID { get; set; }
}

public interface IDetailedOnlineMusicInfo : IBriefOnlineMusicInfo, IDetailedMusicInfoBase
{
    string? CoverUrl { get; set; }
}