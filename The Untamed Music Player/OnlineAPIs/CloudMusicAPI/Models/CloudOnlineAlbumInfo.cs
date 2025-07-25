using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public class BriefCloudOnlineAlbumInfo : IBriefOnlineAlbumInfo
{
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }
    public long ArtistID { get; set; }
    public string ArtistsStr { get; set; } = null!;

    public static async Task<BriefCloudOnlineAlbumInfo> CreateAsync(JsonElement jInfo)
    {
        var info = new BriefCloudOnlineAlbumInfo();
        try
        {
            Task? coverTask = null;
            info.CoverPath = jInfo.GetProperty("picUrl").GetString();
            if (!string.IsNullOrEmpty(info.CoverPath))
            {
                coverTask = LoadCoverAsync(info);
            }
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
            var artistsElement = jInfo.GetProperty("artists");
            if (artistsElement.GetArrayLength() > 0)
            {
                info.ArtistID = artistsElement[0].GetProperty("id").GetInt64();
            }
            string[] artists =
            [
                .. artistsElement
                    .EnumerateArray()
                    .Select(t => t.GetProperty("name").GetString()!)
                    .Distinct()
                    .DefaultIfEmpty(IBriefOnlineAlbumInfo._unknownArtist),
            ];
            info.ArtistsStr = IAlbumInfoBase.GetArtistsStr(artists);
            if (coverTask is not null)
            {
                await coverTask;
            }
            return info;
        }
        catch
        {
            return info;
        }
    }

    public static async Task<BriefCloudOnlineAlbumInfo> CreateFromSongInfoAsync(
        BriefCloudOnlineSongInfo briefInfo
    )
    {
        var api = NeteaseCloudMusicApi.Instance;
        var (_, result) = await api.RequestAsync(
            CloudMusicApiProviders.SongDetail,
            new Dictionary<string, string> { { "ids", $"{briefInfo.ID}" } }
        );
        var info = new BriefCloudOnlineAlbumInfo
        {
            ID = briefInfo.AlbumID,
            Name = briefInfo.Album,
            CoverPath = (string)result["songs"]![0]!["al"]!["picUrl"]!,
        };
        if (!string.IsNullOrEmpty(info.CoverPath))
        {
            await LoadCoverAsync(info);
        }
        return info;
    }

    private static async Task<bool> LoadCoverAsync(BriefCloudOnlineAlbumInfo info)
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

public class DetailedCloudOnlineAlbumInfo : BriefCloudOnlineAlbumInfo, IDetailedOnlineAlbumInfo
{
    public int TotalNum { get; set; } = 0;
    public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero;
    public ushort Year { get; set; } = 0;
    public string DescriptionStr { get; set; } = null!;
    public string? Introduction { get; set; }
    public List<IBriefOnlineSongInfo> SongList { get; set; } = [];

    public static async Task<DetailedCloudOnlineAlbumInfo> CreateAsync(
        BriefCloudOnlineAlbumInfo briefInfo
    )
    {
        var info = new DetailedCloudOnlineAlbumInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            Cover = briefInfo.Cover,
            CoverPath = briefInfo.CoverPath,
            ArtistID = briefInfo.ArtistID,
        };
        try
        {
            var api = NeteaseCloudMusicApi.Instance;
            var (_, result) = await api.RequestAsync(
                CloudMusicApiProviders.Album,
                new Dictionary<string, string> { { "id", $"{briefInfo.ID}" } }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;
            var albumElement = root.GetProperty("album");
            var songsElement = root.GetProperty("songs");
            info.Introduction = albumElement.GetProperty("description").GetString();
            info.Year = (ushort)
                DateTimeOffset
                    .FromUnixTimeMilliseconds(albumElement.GetProperty("publishTime").GetInt64())
                    .Year;
            var artistsElement = albumElement.GetProperty("artists");
            string[] artists =
            [
                .. artistsElement
                    .EnumerateArray()
                    .Select(t => t.GetProperty("name").GetString()!)
                    .Distinct()
                    .DefaultIfEmpty(IBriefOnlineAlbumInfo._unknownArtist),
            ];
            info.ArtistsStr = IAlbumInfoBase.GetArtistsStr(artists);
            await ProcessSongsAsync(songsElement, info, api);
            info.DescriptionStr = IDetailedOnlineAlbumInfo.GetDescriptionStr(
                info.Year,
                info.TotalNum,
                info.TotalDuration
            );
            return info;
        }
        catch
        {
            return info;
        }
    }

    private static async Task ProcessSongsAsync(
        JsonElement songsElement,
        DetailedCloudOnlineAlbumInfo info,
        NeteaseCloudMusicApi api
    )
    {
        var actualCount = songsElement.GetArrayLength();
        if (actualCount == 0)
        {
            return;
        }

        var songIds = Enumerable
            .Range(0, actualCount)
            .Select(i => songsElement[i].GetProperty("id").GetInt64())
            .ToArray();

        var (_, checkResult) = await api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", string.Join(',', songIds) } }
        );
        var data = checkResult["data"]!;

        // 合并可用性和时长映射，减少一次遍历
        var songMetaMap = data.AsArray()
            .ToDictionary(
                item => item!["id"]!.GetValue<long>(),
                item =>
                    (available: item!["url"] is not null, duration: item!["time"]!.GetValue<long>())
            );

        for (var i = 0; i < actualCount; i++)
        {
            var songId = songIds[i];
            if (!songMetaMap.TryGetValue(songId, out var meta) || !meta.available)
            {
                continue;
            }

            try
            {
                var songInfo = new BriefCloudOnlineSongInfo(
                    songsElement[i],
                    meta.available,
                    info.Year,
                    meta.duration
                );
                info.SongList.Add(songInfo);
                info.TotalNum++;
                info.TotalDuration += songInfo.Duration;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}

public class CloudOnlineArtistAlbumInfo : IOnlineArtistAlbumInfo
{
    public bool IsAvailable { get; set; } = false;
    public long ID { get; set; }
    public string Name { get; set; } = null!;
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }
    public string YearStr { get; set; } = null!;
    public List<IBriefSongInfoBase> SongList { get; set; } = [];

    public static async Task<CloudOnlineArtistAlbumInfo> CreateAsync(
        JsonElement jInfo,
        NeteaseCloudMusicApi api
    )
    {
        var info = new CloudOnlineArtistAlbumInfo();
        try
        {
            Task? coverTask = null;
            info.CoverPath = jInfo.GetProperty("picUrl").GetString();
            if (!string.IsNullOrEmpty(info.CoverPath))
            {
                coverTask = LoadCoverAsync(info);
            }
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
            var year = (ushort)
                DateTimeOffset
                    .FromUnixTimeMilliseconds(jInfo.GetProperty("publishTime").GetInt64())
                    .Year;
            info.YearStr = IArtistAlbumInfoBase.GetYearStr(year);
            var (_, result) = await api.RequestAsync(
                CloudMusicApiProviders.Album,
                new Dictionary<string, string> { { "id", $"{info.ID}" } }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;
            var songsElement = root.GetProperty("songs");
            await ProcessSongsAsync(songsElement, info, year, api);
            info.IsAvailable = info.SongList.Count > 0;
            if (!info.IsAvailable)
            {
                return info;
            }
            if (coverTask is not null)
            {
                await coverTask;
            }
            return info;
        }
        catch
        {
            info.IsAvailable = false;
            return info;
        }
    }

    private static async Task ProcessSongsAsync(
        JsonElement songsElement,
        CloudOnlineArtistAlbumInfo info,
        ushort year,
        NeteaseCloudMusicApi api
    )
    {
        var actualCount = songsElement.GetArrayLength();
        if (actualCount == 0)
        {
            return;
        }

        var songIds = Enumerable
            .Range(0, actualCount)
            .Select(i => songsElement[i].GetProperty("id").GetInt64())
            .ToArray();

        var (_, checkResult) = await api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", string.Join(',', songIds) } }
        );
        var data = checkResult["data"]!;

        var songMetaMap = data.AsArray()
            .ToDictionary(
                item => item!["id"]!.GetValue<long>(),
                item =>
                    (available: item!["url"] is not null, duration: item!["time"]!.GetValue<long>())
            );

        for (var i = 0; i < actualCount; i++)
        {
            var songId = songIds[i];
            if (!songMetaMap.TryGetValue(songId, out var meta) || !meta.available)
            {
                continue;
            }

            try
            {
                var songInfo = new BriefCloudOnlineSongInfo(
                    songsElement[i],
                    meta.available,
                    year,
                    meta.duration
                );
                info.SongList.Add(songInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private static async Task<bool> LoadCoverAsync(CloudOnlineArtistAlbumInfo info)
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
