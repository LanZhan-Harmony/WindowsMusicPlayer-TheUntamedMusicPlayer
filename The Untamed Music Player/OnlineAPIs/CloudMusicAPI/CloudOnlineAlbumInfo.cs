using System.Text.Json.Nodes;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public class CloudOnlineAlbumInfo : IOnlineAlbumInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }
    public long[] Artists { get; set; } = null!;
    public string ArtistsStr { get; set; } = null!;
    public int TotalNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ushort Year { get; set; }
    public long ModifiedDate { get; set; }

    public static async Task<CloudOnlineAlbumInfo> CreateAsync(JsonObject jInfo)
    {
        return new CloudOnlineAlbumInfo();
    }

    public byte[] GetCoverBytes()
    {
        return [];
    }
    public string GetDescriptionStr()
    {
        return "";
    }
}
