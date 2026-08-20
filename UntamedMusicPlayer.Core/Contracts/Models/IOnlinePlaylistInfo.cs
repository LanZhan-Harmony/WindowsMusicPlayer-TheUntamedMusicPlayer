using UntamedMusicPlayer.Core.Helpers;

namespace UntamedMusicPlayer.Core.Contracts.Models;

public interface IBriefOnlinePlaylistInfo
{
    long ID { get; set; }
    string Name { get; set; }
    string TotalSongNumStr { get; set; }
    string? CoverPath { get; set; }

    static string GetTotalSongNumStr(int totalSongNum)
    {
        return totalSongNum == 1
            ? $"{totalSongNum} {"PlaylistInfo_Item".GetLocalized()}"
            : $"{totalSongNum} {"PlaylistInfo_Items".GetLocalized()}";
    }
}

public interface IDetailedOnlinePlaylistInfo : IBriefOnlinePlaylistInfo
{
    string? Introduction { get; set; }
    List<IBriefOnlineSongInfo> SongList { get; set; }

}
