using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlineAlbumInfo : IBriefOnlineAlbumInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }
    public string ArtistsStr { get; set; } = null!;

    public static async Task<BriefCloudOnlineAlbumInfo> CreateAsync(JsonElement jInfo)
    {
        var info = new BriefCloudOnlineAlbumInfo();
        try
        {
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
            info.CoverPath = jInfo.GetProperty("picUrl").GetString();
            var artistsElement = jInfo.GetProperty("artists");
            string[] artists =
            [
                .. artistsElement
                    .EnumerateArray()
                    .Select(t => t.GetProperty("name").GetString()!)
                    .Distinct()
                    .DefaultIfEmpty(IBriefOnlineAlbumInfo._unknownArtist),
            ];
            info.ArtistsStr = IBriefOnlineAlbumInfo.GetArtistsStr(artists);
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

            // 等待 UI 线程操作完成再释放资源
            await tcs.Task;
            return info;
        }
        catch
        {
            return info;
        }
    }
}

public class DetailedCloudOnlineAlbumInfo : BriefCloudOnlineAlbumInfo, IDetailedOnlineAlbumInfo
{
    public int TotalNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ushort Year { get; set; }
    public string? Description { get; set; }
}
