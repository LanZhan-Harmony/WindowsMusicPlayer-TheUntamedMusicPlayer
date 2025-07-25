using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;

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

    public static async Task<IBriefOnlineArtistInfo> CreateFromSongInfoAsync(
        CloudBriefOnlineSongInfo briefInfo
    )
    {
        var api = NeteaseCloudMusicApi.Instance;
        var (_, result) = await api.RequestAsync(
            CloudMusicApiProviders.ArtistAlbum,
            new Dictionary<string, string>
            {
                { "id", $"{briefInfo.ArtistID}" },
                { "limit", "0" },
                { "offset", "0" },
            }
        );
        var info = new BriefCloudOnlineArtistInfo
        {
            ID = briefInfo.ArtistID,
            Name = (string)result["artist"]!["name"]!,
            CoverPath = (string)result["artist"]!["picUrl"]!,
        };
        if (!string.IsNullOrEmpty(info.CoverPath))
        {
            await LoadCoverAsync(info);
        }
        return info;
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
    private readonly HashSet<long> ArtistAlbumIDs = [];
    public const byte Limit = 10;
    public ushort Page { get; set; } = 0;
    public int CurrentAlbumNum { get; set; } = 0;
    public bool HasAllLoaded { get; set; } = false;
    public int TotalAlbumNum { get; set; }
    public int TotalSongNum { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public string CountStr { get; set; } = null!;
    public string DescriptionStr { get; set; } = $"{"ArtistInfo_Artist".GetLocalized()} ";
    public string? Introduction { get; set; }
    public ObservableCollection<IOnlineArtistAlbumInfo> AlbumList { get; set; } = [];

    public void Add(IOnlineArtistAlbumInfo? info)
    {
        CurrentAlbumNum++;
        if (info is not null && info.IsAvailable && ArtistAlbumIDs.Add(info.ID))
        {
            AlbumList.Add(info);
        }
        if (TotalAlbumNum == CurrentAlbumNum)
        {
            HasAllLoaded = true;
        }
    }
}
