using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

[MemoryPackable]
public partial class CloudBriefOnlineSongInfo : IBriefOnlineSongInfo
{
    public int PlayQueueIndex { get; set; } = -1;
    public bool IsPlayAvailable { get; set; } = false;
    public string Path { get; set; } = null!;
    public string Title { get; set; } = "";
    public long ID { get; set; } = 0;
    public virtual string Album { get; set; } = "";
    public long AlbumID { get; set; } = 0;
    public virtual string ArtistsStr { get; set; } = "";
    public long ArtistID { get; set; } = 0;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public virtual string DurationStr { get; set; } = "";
    public string YearStr { get; set; } = "";
    public string GenreStr { get; set; } = "";

    [MemoryPackConstructor]
    public CloudBriefOnlineSongInfo() { }

    public CloudBriefOnlineSongInfo(JsonElement jInfo, bool isAvailable)
    {
        IsPlayAvailable = isAvailable;
        if (!isAvailable)
        {
            return;
        }
        try
        {
            ID = jInfo.GetProperty("id").GetInt64();
            var albumElement = jInfo.GetProperty("album");
            Title = jInfo.GetProperty("name").GetString()!;
            var album = albumElement.GetProperty("name").GetString()!;
            Album = string.IsNullOrWhiteSpace(album) ? IBriefSongInfoBase._unknownAlbum : album;
            AlbumID = albumElement.GetProperty("id").GetInt64();
            var artistsElement = jInfo.GetProperty("artists");
            if (artistsElement.GetArrayLength() > 0)
            {
                ArtistID = artistsElement[0].GetProperty("id").GetInt64();
            }
            string[] artists =
            [
                .. artistsElement
                    .EnumerateArray()
                    .Select(t => t.GetProperty("name").GetString()!)
                    .Distinct()
                    .DefaultIfEmpty(IBriefSongInfoBase._unknownArtist),
            ];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(artists);
            Duration = TimeSpan.FromMilliseconds(jInfo.GetProperty("duration").GetInt64());
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            YearStr = IBriefSongInfoBase.GetYearStr(
                (ushort)
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(
                            albumElement.GetProperty("publishTime").GetInt64()
                        )
                        .Year
            );
        }
        catch
        {
            IsPlayAvailable = false;
        }
    }

    /// <summary>
    /// 从OnlineAlbumInfo创建的构造函数
    /// </summary>
    /// <param name="jInfo"></param>
    /// <param name="isAvailable"></param>
    /// <param name="year"></param>
    /// <param name="duration"></param>
    public CloudBriefOnlineSongInfo(JsonElement jInfo, bool isAvailable, ushort year, long duration)
    {
        IsPlayAvailable = isAvailable;
        if (!isAvailable)
        {
            return;
        }
        ID = jInfo.GetProperty("id").GetInt64();
        Title = jInfo.GetProperty("name").GetString()!;
        var albumElement = jInfo.GetProperty("al");
        var album = albumElement.GetProperty("name").GetString()!;
        Album = string.IsNullOrWhiteSpace(album) ? IBriefSongInfoBase._unknownAlbum : album;
        AlbumID = albumElement.GetProperty("id").GetInt64();
        var artistsElement = jInfo.GetProperty("ar");
        if (artistsElement.GetArrayLength() > 0)
        {
            ArtistID = artistsElement[0].GetProperty("id").GetInt64();
        }
        string[] artists =
        [
            .. artistsElement
                .EnumerateArray()
                .Select(t => t.GetProperty("name").GetString()!)
                .Distinct()
                .DefaultIfEmpty(IBriefSongInfoBase._unknownArtist),
        ];
        ArtistsStr = IBriefSongInfoBase.GetArtistsStr(artists);
        YearStr = IBriefSongInfoBase.GetYearStr(year);
        Duration = TimeSpan.FromMilliseconds(duration);
        DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public class CloudDetailedOnlineSongInfo : CloudBriefOnlineSongInfo, IDetailedOnlineSongInfo
{
    public bool IsOnline { get; set; } = true;
    public string AlbumArtistsStr { get; set; } = "";
    public string ArtistAndAlbumStr { get; set; } = "";
    public BitmapImage? Cover { get; set; }
    public string? CoverPath { get; set; }
    public string ItemType { get; set; } = "";
    public string BitRate { get; set; } = "";
    public string Track { get; set; } = "";
    public string Lyric { get; set; } = "";

    public CloudDetailedOnlineSongInfo() { }

    public static async Task<CloudDetailedOnlineSongInfo> CreateAsync(CloudBriefOnlineSongInfo info)
    {
        var detailedInfo = new CloudDetailedOnlineSongInfo
        {
            IsPlayAvailable = info.IsPlayAvailable,
            ID = info.ID,
            Title = info.Title,
            AlbumID = info.AlbumID,
            DurationStr = info.DurationStr,
            YearStr = info.YearStr,
        };
        var api = NeteaseCloudMusicApi.Instance;
        var songUrlTask = api.RequestAsync(
            CloudMusicApiProviders.SongUrl,
            new Dictionary<string, string> { { "id", $"{info.ID}" } }
        );
        var albumTask = api.RequestAsync(
            CloudMusicApiProviders.Album,
            new Dictionary<string, string> { { "id", $"{info.AlbumID}" } }
        );
        var lyricTask = api.RequestAsync(
            CloudMusicApiProviders.Lyric,
            new Dictionary<string, string> { { "id", $"{info.ID}" } }
        );
        await Task.WhenAll(songUrlTask, albumTask, lyricTask);
        var (_, songUrlResult) = songUrlTask.Result;
        var (_, albumResult) = albumTask.Result;
        var (_, lyricResult) = lyricTask.Result;
        try
        {
            detailedInfo.Path = (string)songUrlResult["data"]![0]!["url"]!; // 临时链接可能过期, 所以重新获取
            Task? coverTask = null;
            if (info.Album != IBriefSongInfoBase._unknownAlbum)
            {
                detailedInfo.Album = info.Album;
                detailedInfo.CoverPath = (string)albumResult["album"]!["picUrl"]!;
                if (!string.IsNullOrEmpty(detailedInfo.CoverPath))
                {
                    coverTask = LoadCoverAsync(detailedInfo);
                }
                string[] albumArtists =
                [
                    .. ((JsonArray)albumResult["album"]!["artists"]!)
                        .Select(t => (string)t!["name"]!)
                        .Distinct(),
                ];
                detailedInfo.AlbumArtistsStr = IDetailedSongInfoBase.GetAlbumArtistsStr(
                    albumArtists
                );
            }
            detailedInfo.ArtistsStr =
                info.ArtistsStr == IBriefSongInfoBase._unknownArtist ? "" : info.ArtistsStr;
            detailedInfo.ArtistAndAlbumStr = IDetailedSongInfoBase.GetArtistAndAlbumStr(
                detailedInfo.Album,
                detailedInfo.ArtistsStr
            );
            detailedInfo.ItemType = $".{(string)songUrlResult["data"]![0]!["type"]!}";
            detailedInfo.BitRate = $"{((int)songUrlResult["data"]![0]!["br"]!) / 1000} kbps";
            detailedInfo.Lyric = (string)lyricResult["lrc"]!["lyric"]!;
            if (coverTask is not null)
            {
                await coverTask;
            }
            return detailedInfo;
        }
        catch (NullReferenceException)
        {
            if (string.IsNullOrWhiteSpace(detailedInfo.Path))
            {
                detailedInfo.IsPlayAvailable = false;
            }
            return detailedInfo;
        }
        catch (Exception ex)
        {
            detailedInfo.IsPlayAvailable = false;
            Debug.WriteLine(ex.StackTrace);
            return detailedInfo;
        }
    }

    private static async Task<bool> LoadCoverAsync(CloudDetailedOnlineSongInfo info)
    {
        try
        {
            using var httpClient = new HttpClient();
            var coverBuffer = await httpClient.GetByteArrayAsync(info.CoverPath);
            var stream = new MemoryStream(coverBuffer);
            var tcs = new TaskCompletionSource<bool>();
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var bitmap = new BitmapImage { DecodePixelWidth = 400 };
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
            });
            return await tcs.Task;
        }
        catch
        {
            return false;
        }
    }
}
