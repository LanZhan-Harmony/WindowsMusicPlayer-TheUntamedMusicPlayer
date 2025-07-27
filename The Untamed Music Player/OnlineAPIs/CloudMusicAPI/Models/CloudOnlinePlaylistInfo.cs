using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlinePlaylistInfo : IBriefOnlinePlaylistInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public string TotalSongNumStr { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }

    public static async Task<BriefCloudOnlinePlaylistInfo> CreateAsync(JsonElement jInfo)
    {
        var info = new BriefCloudOnlinePlaylistInfo();
        try
        {
            Task? coverTask = null;
            info.CoverPath = jInfo.GetProperty("coverImgUrl").GetString();
            if (!string.IsNullOrEmpty(info.CoverPath))
            {
                coverTask = LoadCoverAsync(info);
            }
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
            var totalSongNum = jInfo.GetProperty("trackCount").GetInt32();
            info.TotalSongNumStr = IBriefOnlinePlaylistInfo.GetTotalSongNumStr(totalSongNum);
            if (coverTask is not null)
            {
                await coverTask;
            }
        }
        catch { }
        return info;
    }

    private static async Task<bool> LoadCoverAsync(BriefCloudOnlinePlaylistInfo info)
    {
        try
        {
            using var httpClient = new HttpClient();
            var coverBytes = await httpClient.GetByteArrayAsync(info.CoverPath);
            var stream = new MemoryStream(coverBytes);
            var tcs = new TaskCompletionSource<bool>();
            App.MainWindow?.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                async () =>
                {
                    try
                    {
                        var bitmap = new BitmapImage { DecodePixelWidth = 160 };
                        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                        info.Cover = bitmap;
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        stream.Dispose();
                    }
                }
            );

            return await tcs.Task;
        }
        catch
        {
            return false;
        }
    }
}

public class DetailedCloudOnlinePlaylistInfo
    : BriefCloudOnlinePlaylistInfo,
        IDetailedOnlinePlaylistInfo
{
    public string? Introduction { get; set; }
    public List<IBriefOnlineSongInfo> SongList { get; set; } = [];

    public static async Task<DetailedCloudOnlinePlaylistInfo> CreateAsync(
        BriefCloudOnlinePlaylistInfo briefInfo
    )
    {
        var info = new DetailedCloudOnlinePlaylistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            TotalSongNumStr = briefInfo.TotalSongNumStr,
            Cover = briefInfo.Cover,
            CoverPath = briefInfo.CoverPath,
        };
        try
        {
            var api = NeteaseCloudMusicApi.Instance;
            var (_, result) = await api.RequestAsync(
                CloudMusicApiProviders.PlaylistDetail,
                new Dictionary<string, string> { { "id", $"{briefInfo.ID}" } }
            );
        }
        catch { }
        return info;
    }
}
