using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlineArtistInfo : IBriefOnlineArtistInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }

    public static async Task<BriefCloudOnlineArtistInfo> CreateAsync(JsonElement jInfo)
    {
        var info = new BriefCloudOnlineArtistInfo();
        try
        {
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
            info.CoverPath = jInfo.GetProperty("picUrl").GetString();
            using var httpClient = new HttpClient();
            var coverBytes = await httpClient.GetByteArrayAsync(info.CoverPath);
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(coverBytes.AsBuffer());
            stream.Seek(0);
            var tcs = new TaskCompletionSource<bool>();
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var bitmap = new BitmapImage { DecodePixelWidth = 160 };
                    await bitmap.SetSourceAsync(stream);
                    info.Cover = bitmap;
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await tcs.Task;
            return info;
        }
        catch
        {
            return info;
        }
    }
}

public class DetailedCloudOnlineArtistInfo : BriefCloudOnlineArtistInfo, IDetailedOnlineArtistInfo
{
    public int TotalAlbumNum { get; set; }
    public int TotalSongNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public string? Description { get; set; }
}
