using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Contracts.Models;

public interface IBriefOnlinePlaylistInfo
{
    long ID { get; set; }
    string Name { get; set; }
    string TotalSongNumStr { get; set; }
    BitmapImage? Cover { get; set; }
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

    static async Task<IDetailedOnlinePlaylistInfo> CreateDetailedOnlinePlaylistInfoAsync(
        IBriefOnlinePlaylistInfo info
    )
    {
        return info switch
        {
            BriefCloudOnlinePlaylistInfo cloudInfo =>
                await DetailedCloudOnlinePlaylistInfo.CreateAsync(cloudInfo),
            _ => await DetailedCloudOnlinePlaylistInfo.CreateAsync(
                (BriefCloudOnlinePlaylistInfo)info
            ),
        };
    }
}
