using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

[MemoryPackable]
public partial class CloudBriefOnlineSongInfo : IBriefOnlineSongInfo
{
    protected static readonly string _unknownAlbum = "SongInfo_UnknownAlbum".GetLocalized();
    protected static readonly string _unknownArtist = "SongInfo_UnknownArtist".GetLocalized();

    public int PlayQueueIndex { get; set; } = -1;
    public bool IsPlayAvailable { get; set; } = false;
    public string Path { get; set; } = null!;
    public string Title { get; set; } = "";
    public long ID { get; set; } = 0;
    public virtual string Album { get; set; } = "";
    public long AlbumID { get; set; } = 0;
    public virtual string ArtistsStr { get; set; } = "";
    public virtual string DurationStr { get; set; } = "";
    public string YearStr { get; set; } = "";
    public string GenreStr { get; set; } = "";

    [MemoryPackConstructor]
    public CloudBriefOnlineSongInfo() { }

    public static async Task<CloudBriefOnlineSongInfo> CreateAsync(
        JsonElement jInfo,
        NeteaseCloudMusicApi api
    )
    {
        var info = new CloudBriefOnlineSongInfo();
        try
        {
            info.ID = jInfo.GetProperty("id").GetInt64();

            var (_, songUrlResult) = await api.RequestAsync(
                CloudMusicApiProviders.SongUrl,
                new Dictionary<string, string> { { "id", $"{info.ID}" } }
            );
            if (songUrlResult["data"]![0]!["url"] is null)
            {
                info.IsPlayAvailable = false;
                return info;
            }

            var albumElement = jInfo.GetProperty("album");
            info.Title = jInfo.GetProperty("name").GetString()!;
            var album = albumElement.GetProperty("name").GetString()!;
            info.Album = string.IsNullOrWhiteSpace(album) ? _unknownAlbum : album;
            info.AlbumID = albumElement.GetProperty("id").GetInt64();
            var artistsElement = jInfo.GetProperty("artists");
            string[] artists =
            [
                .. artistsElement
                    .EnumerateArray()
                    .Select(t => t.GetProperty("name").GetString()!)
                    .Distinct()
                    .DefaultIfEmpty(_unknownArtist),
            ];
            info.ArtistsStr = IBriefSongInfoBase.GetArtistsStr(artists);
            info.DurationStr = IBriefSongInfoBase.GetDurationStr(
                TimeSpan.FromMilliseconds(jInfo.GetProperty("duration").GetInt64())
            );
            info.YearStr = IBriefSongInfoBase.GetYearStr(
                (ushort)
                    DateTimeOffset
                        .FromUnixTimeMilliseconds(
                            albumElement.GetProperty("publishTime").GetInt64()
                        )
                        .Year
            );

            info.IsPlayAvailable = true;
            return info;
        }
        catch
        {
            info.IsPlayAvailable = false;
            return info;
        }
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

    public static async Task<CloudDetailedOnlineSongInfo> CreateAsync(IBriefOnlineSongInfo info)
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
        api.Dispose();
        try
        {
            detailedInfo.Path = (string)songUrlResult["data"]![0]!["url"]!; // 临时链接可能过期, 所以重新获取
            Task? coverTask = null;
            if (info.Album != _unknownAlbum)
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
            detailedInfo.ArtistsStr = info.ArtistsStr == _unknownArtist ? "" : info.ArtistsStr;
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
        using var httpClient = new HttpClient();
        var coverBuffer = await httpClient.GetByteArrayAsync(info.CoverPath);
        var tcs = new TaskCompletionSource<bool>();
        App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(coverBuffer.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage { DecodePixelWidth = 400 };
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
