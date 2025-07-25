using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlinePlaylistInfo
{
    long ID { get; set; }
    string Name { get; set; }
    int TotalSongNum { get; set; }
    BitmapImage Cover { get; set; }
    string CoverPath { get; set; }

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
}

public interface IDetailedOnlinePlaylistInfo : IBriefOnlinePlaylistInfo
{
    string CreatorName { get; set; }
    string? Introduction { get; set; }
    List<IBriefOnlineSongInfo> SongList { get; set; }
}
