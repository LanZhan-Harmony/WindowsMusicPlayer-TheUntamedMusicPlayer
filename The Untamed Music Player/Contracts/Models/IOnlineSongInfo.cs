namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineSongInfo : IBriefSongInfoBase
{
    long ID { get; set; }
}

public interface IDetailedOnlineSongInfo : IBriefOnlineSongInfo, IDetailedSongInfoBase
{
    string? CoverPath { get; set; }
}
