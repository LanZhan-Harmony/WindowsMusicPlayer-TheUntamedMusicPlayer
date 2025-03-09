using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public class CloudBriefOnlineMusicInfo : IBriefOnlineMusicInfo
{
    public int PlayQueueIndex { get; set; } = -1;
    public bool IsAvailable { get; set; } = false;
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public long ID { get; set; } = 0;
    public virtual string Album { get; set; } = "";
    public long AlbumID { get; set; } = 0;
    public virtual string ArtistsStr { get; set; } = "";
    public virtual string DurationStr { get; set; } = "";
    public string YearStr { get; set; } = "";
    public string GenreStr { get; set; } = "";

    public CloudBriefOnlineMusicInfo() { }

    public static async Task<CloudBriefOnlineMusicInfo> CreateAsync(JsonNode jInfo, NeteaseCloudMusicApi api)
    {
        var info = new CloudBriefOnlineMusicInfo();
        try
        {
            info.ID = (long)jInfo["id"]!;
            var (isOK, songUrlResult) = await api.RequestAsync(CloudMusicApiProviders.SongUrl, new Dictionary<string, string> { { "id", $"{info.ID}" } });
            if (songUrlResult["data"] is null || songUrlResult["data"]![0]!["url"] is null)
            {
                info.IsAvailable = false;
                return info;
            }
            else
            {
                info.Title = (string)jInfo["name"]!;
                info.Album = (string)jInfo["album"]!["name"]!;
                info.AlbumID = (long)jInfo["album"]!["id"]!;
                string[] artists = [.. ((JsonArray)jInfo["artists"]!)
                    .Select(t => (string)t!["name"]!)
                    .Distinct()];
                info.ArtistsStr = IBriefMusicInfoBase.GetArtistsStr(artists);
                info.DurationStr = IBriefMusicInfoBase.GetDurationStr(TimeSpan.FromMilliseconds((long)jInfo["duration"]!));
                info.YearStr = IBriefMusicInfoBase.GetYearStr((ushort)DateTimeOffset.FromUnixTimeMilliseconds((long)jInfo["album"]!["publishTime"]!).Year);
                info.IsAvailable = true;
                return info;
            }
        }
        catch
        {
            info.IsAvailable = false;
            return info;
        }
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public class CloudDetailedOnlineMusicInfo : CloudBriefOnlineMusicInfo, IDetailedOnlineMusicInfo
{
    public bool IsPlayAvailable { get; set; } = true;
    public bool IsOnline { get; set; } = true;
    public string AlbumArtistsStr { get; set; } = "";
    public string ArtistAndAlbumStr { get; set; } = "";
    public BitmapImage? Cover { get; set; }
    public string? CoverUrl { get; set; }
    public string ItemType { get; set; } = "";
    public string BitRate { get; set; } = "";
    public string Track { get; set; } = "";
    public string Lyric { get; set; } = "";

    public CloudDetailedOnlineMusicInfo() { }

    public static async Task<CloudDetailedOnlineMusicInfo> CreateAsync(IBriefOnlineMusicInfo info)
    {
        var detailedInfo = new CloudDetailedOnlineMusicInfo
        {
            ID = info.ID,
            Title = info.Title,
            Album = info.Album,
            AlbumID = info.AlbumID,
            ArtistsStr = info.ArtistsStr,
            DurationStr = info.DurationStr,
            YearStr = info.YearStr,
        };
        var api = new NeteaseCloudMusicApi();
        var songUrlTask = api.RequestAsync(CloudMusicApiProviders.SongUrl, new Dictionary<string, string> { { "id", $"{info.ID}" } });
        var albumTask = api.RequestAsync(CloudMusicApiProviders.Album, new Dictionary<string, string> { { "id", $"{info.AlbumID}" } });
        var lyricTask = api.RequestAsync(CloudMusicApiProviders.Lyric, new Dictionary<string, string> { { "id", $"{info.ID}" } });
        await Task.WhenAll(songUrlTask, albumTask, lyricTask);
        var (isOK1, songUrlResult) = songUrlTask.Result;
        var (isOK2, albumResult) = albumTask.Result;
        var (isOK3, lyricResult) = lyricTask.Result;
        api.Dispose();
        try
        {
            detailedInfo.CoverUrl = (string)albumResult["album"]!["picUrl"]!;
            Task? coverTask = null;
            if (!string.IsNullOrEmpty(detailedInfo.CoverUrl))
            {
                coverTask = LoadCoverAsync(detailedInfo);
            }
            detailedInfo.Path = (string)songUrlResult["data"]![0]!["url"]!; // 临时链接可能过期, 所以重新获取
            detailedInfo.ItemType = $".{(string)songUrlResult["data"]![0]!["type"]!}";
            string[] albumArtists = [.. ((JsonArray)albumResult["album"]!["artists"]!)
                    .Select(t => (string)t!["name"]!)
                    .Distinct()];
            detailedInfo.AlbumArtistsStr = IDetailedMusicInfoBase.GetAlbumArtistsStr(albumArtists);
            detailedInfo.ArtistAndAlbumStr = IDetailedMusicInfoBase.GetArtistAndAlbumStr(detailedInfo.Album, detailedInfo.ArtistsStr);
            detailedInfo.BitRate = $"{((int)songUrlResult["data"]![0]!["br"]!) / 1000} kbps";
            detailedInfo.Lyric = (string)lyricResult["lrc"]!["lyric"]!;
            if (coverTask is not null)
            {
                await coverTask;
            }
            return detailedInfo;
        }
        catch (Exception ex) when (ex is NullReferenceException)
        {
            detailedInfo.IsPlayAvailable = false;
            return detailedInfo;
        }
        catch (Exception ex)
        {
            detailedInfo.IsAvailable = false;
            Debug.WriteLine(ex.StackTrace);
            return detailedInfo;
        }
    }

    private static async Task<bool> LoadCoverAsync(CloudDetailedOnlineMusicInfo info)
    {
        using var httpClient = new HttpClient();
        var coverBuffer = await httpClient.GetByteArrayAsync(info.CoverUrl);
        var tcs = new TaskCompletionSource<bool>();
        App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(coverBuffer.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage
                {
                    DecodePixelWidth = 400
                };
                await bitmap.SetSourceAsync(stream);
                info.Cover = bitmap;
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return await tcs.Task;
    }
}
