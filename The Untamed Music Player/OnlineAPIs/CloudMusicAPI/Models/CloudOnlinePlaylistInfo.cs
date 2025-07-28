using System.Diagnostics;
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
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;
            var playlistElement = root.GetProperty("playlist");
            info.Introduction = playlistElement.GetProperty("description").GetString();
            var tracksElement = playlistElement.GetProperty("trackIds");
            var actualCount = tracksElement.GetArrayLength();
            if (actualCount == 0)
            {
                return info;
            }
            var trackIds = tracksElement
                .EnumerateArray()
                .Select(t => t.GetProperty("id").GetInt64())
                .ToArray();

            var (_, checkResult) = await api.RequestAsync(
                CloudMusicApiProviders.SongUrl,
                new Dictionary<string, string> { { "id", string.Join(',', trackIds) } }
            );
            var (_, detailsResult) = await api.RequestAsync(
                CloudMusicApiProviders.SongDetail,
                new Dictionary<string, string> { { "ids", string.Join(',', trackIds) } }
            );
            var data = checkResult["data"]!;
            var availabilityMap = data.AsArray()
                .ToDictionary(
                    item => item!["id"]!.GetValue<long>(),
                    item => item!["url"] is not null
                );
            var detailsMap = detailsResult["songs"]!
                .AsArray()
                .ToDictionary(item => item!["id"]!.GetValue<long>(), item => item);

            for (var i = 0; i < actualCount; i++)
            {
                var songId = trackIds[i];
                var available = availabilityMap.GetValueOrDefault(songId, false);
                if (!available)
                {
                    continue;
                }

                var trackElement = detailsMap.GetValueOrDefault(songId)!;

                try
                {
                    var songInfo = new BriefCloudOnlineSongInfo(trackElement);
                    info.SongList.Add(songInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        }
        catch { }
        return info;
    }
}
