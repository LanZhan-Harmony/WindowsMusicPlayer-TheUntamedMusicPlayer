using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlinePlaylistInfo
{
    long ID { get; set; }
    string Name { get; set; }
    string TotalSongNumStr { get; set; }
    BitmapImage? Cover { get; set; }
    string? CoverPath { get; set; }

    static async Task<byte[]> GetCoverBytes(IBriefOnlinePlaylistInfo info)
    {
        if (info.Cover is not null)
        {
            try
            {
                using var httpClient = new HttpClient();
                return await httpClient.GetByteArrayAsync(info.CoverPath);
            }
            catch { }
        }
        return [];
    }

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
