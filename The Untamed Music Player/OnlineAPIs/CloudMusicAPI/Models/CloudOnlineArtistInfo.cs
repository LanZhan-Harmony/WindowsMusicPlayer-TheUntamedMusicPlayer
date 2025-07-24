using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
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
            Task? coverTask = null;
            info.CoverPath = jInfo.GetProperty("picUrl").GetString();
            if (!string.IsNullOrEmpty(info.CoverPath))
            {
                coverTask = LoadCoverAsync(info);
            }
            info.ID = jInfo.GetProperty("id").GetInt64();
            info.Name = jInfo.GetProperty("name").GetString()!;
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

    private static async Task<bool> LoadCoverAsync(BriefCloudOnlineArtistInfo info)
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

public class DetailedCloudOnlineArtistInfo : BriefCloudOnlineArtistInfo, IDetailedOnlineArtistInfo
{
    private const byte _limit = 10;

    public int TotalAlbumNum { get; set; }
    public int TotalSongNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public string? Introduction { get; set; }
    public List<IOnlineArtistAlbumInfo> AlbumList { get; set; } = [];

    public static async Task<DetailedCloudOnlineArtistInfo> CreateAsync(
        BriefCloudOnlineArtistInfo briefInfo
    )
    {
        var info = new DetailedCloudOnlineArtistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            Cover = briefInfo.Cover,
            CoverPath = briefInfo.CoverPath,
        };
        try
        {
            var api = NeteaseCloudMusicApi.Instance;
            var albumTask = api.RequestAsync(
                CloudMusicApiProviders.ArtistAlbum,
                new Dictionary<string, string>
                {
                    { "id", $"{briefInfo.ID}" },
                    { "limit", $"{_limit}" },
                    { "offset", "0" },
                }
            );
            var artistTask = api.RequestAsync(
                CloudMusicApiProviders.ArtistDesc,
                new Dictionary<string, string> { { "id", $"{briefInfo.ID}" } }
            );
            await Task.WhenAll(albumTask, artistTask);
            var (_, albumResult) = albumTask.Result;
            var (_, artistResult) = artistTask.Result;

            info.Introduction = artistResult["briefDesc"]?.ToString();

            using var document = JsonDocument.Parse(albumResult.ToJsonString());
            var root = document.RootElement;
            var artistElement = root.GetProperty("artist");
            info.TotalAlbumNum = artistElement.GetProperty("albumSize").GetInt32();
            info.TotalSongNum = artistElement.GetProperty("musicSize").GetInt32();
            var albumsElement = root.GetProperty("hotAlbums");
            var actualCount = albumsElement.GetArrayLength();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await Parallel.ForEachAsync(
                Enumerable.Range(0, actualCount),
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount / 2,
                    CancellationToken = cts.Token,
                },
                async (i, cancellationToken) =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var albumInfo = await CloudArtistAlbumInfo.CreateAsync(
                            albumsElement[i]!,
                            api
                        );
                        info.AlbumList.Add(albumInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            );
            return info;
        }
        catch
        {
            return info;
        }
    }

    public string GetCountStr()
    {
        var albumStr =
            TotalAlbumNum > 1
                ? "ArtistInfo_Albums".GetLocalized()
                : "ArtistInfo_Album".GetLocalized();
        var songStr =
            TotalSongNum > 1 ? "AlbumInfo_Songs".GetLocalized() : "AlbumInfo_Song".GetLocalized();
        return $"{TotalAlbumNum} {albumStr} • {TotalSongNum} {songStr} •";
    }

    public string GetDurationStr()
    {
        var hourStr =
            TotalDuration.Hours > 1
                ? "ArtistInfo_Hours".GetLocalized()
                : "ArtistInfo_Hour".GetLocalized();
        var minuteStr =
            TotalDuration.Minutes > 1
                ? "ArtistInfo_Mins".GetLocalized()
                : "ArtistInfo_Min".GetLocalized();
        var secondStr =
            TotalDuration.Seconds > 1
                ? "ArtistInfo_Secs".GetLocalized()
                : "ArtistInfo_Sec".GetLocalized();

        return TotalDuration.Hours > 0
            ? $"{TotalDuration.Hours} {hourStr} {TotalDuration.Minutes} {minuteStr} {TotalDuration.Seconds} {secondStr}"
            : $"{TotalDuration.Minutes} {minuteStr} {TotalDuration.Seconds} {secondStr}";
    }
}
