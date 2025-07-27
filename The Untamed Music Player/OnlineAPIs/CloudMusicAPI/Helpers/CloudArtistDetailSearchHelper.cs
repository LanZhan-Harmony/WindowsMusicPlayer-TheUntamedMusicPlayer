using System.Diagnostics;
using System.Text.Json;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudArtistDetailSearchHelper
{
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task<DetailedCloudOnlineArtistInfo> SearchArtistDetailAsync(
        BriefCloudOnlineArtistInfo briefInfo
    )
    {
        await _searchSemaphore.WaitAsync();
        var info = new DetailedCloudOnlineArtistInfo
        {
            ID = briefInfo.ID,
            Name = briefInfo.Name,
            Cover = briefInfo.Cover,
            CoverPath = briefInfo.CoverPath,
        };
        try
        {
            var albumTask = _api.RequestAsync(
                CloudMusicApiProviders.ArtistAlbum,
                new Dictionary<string, string>
                {
                    { "id", $"{briefInfo.ID}" },
                    { "limit", $"{DetailedCloudOnlineArtistInfo.Limit}" },
                    { "offset", "0" },
                }
            );
            var artistTask = _api.RequestAsync(
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
            var albumsElement = root.GetProperty("hotAlbums");
            info.TotalAlbumNum = artistElement.GetProperty("albumSize").GetInt32();
            info.TotalSongNum = artistElement.GetProperty("musicSize").GetInt32();
            info.CountStr = IDetailedOnlineArtistInfo.GetCountStr(
                info.TotalAlbumNum,
                info.TotalSongNum
            );
            if (info.TotalAlbumNum == 0)
            {
                info.HasAllLoaded = true;
                return info;
            }
            await ProcessArtistDetailAsync(albumsElement, info);
            info.Page = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
        return info;
    }

    public static async Task SearchMoreArtistDetailAsync(DetailedCloudOnlineArtistInfo info)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.ArtistAlbum,
                new Dictionary<string, string>
                {
                    { "id", $"{info.ID}" },
                    { "limit", $"{DetailedCloudOnlineArtistInfo.Limit}" },
                    { "offset", $"{info.Page * DetailedCloudOnlineArtistInfo.Limit}" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;
            var albumsElement = root.GetProperty("hotAlbums");
            await ProcessArtistDetailAsync(albumsElement, info);
            info.Page++;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    private static async Task ProcessArtistDetailAsync(
        JsonElement albumsElement,
        DetailedCloudOnlineArtistInfo info
    )
    {
        var actualCount = albumsElement.GetArrayLength();
        var albumInfos = new CloudOnlineArtistAlbumInfo[actualCount];
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
                    var albumInfo = await CloudOnlineArtistAlbumInfo.CreateAsync(
                        albumsElement[i]!,
                        _api,
                        true
                    );
                    albumInfos[i] = albumInfo;
                }
                catch (Exception ex)
                {
                    lock (info.AlbumList)
                    {
                        info.CurrentAlbumNum++;
                    }
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        );

        foreach (var albumInfo in albumInfos)
        {
            info.Add(albumInfo);
        }
    }

    public static async Task<List<IBriefSongInfoBase>> GetSongsByArtistAsync(
        BriefCloudOnlineArtistInfo info
    )
    {
        var songs = new List<IBriefSongInfoBase>();
        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.ArtistAlbum,
                new Dictionary<string, string>
                {
                    { "id", $"{info.ID}" },
                    { "limit", $"{DetailedCloudOnlineArtistInfo.Limit}" },
                    { "offset", $"0" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;
            var albumsElement = root.GetProperty("hotAlbums");
            var actualCount = albumsElement.GetArrayLength();
            var albumInfos = new CloudOnlineArtistAlbumInfo[actualCount];
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
                        var albumInfo = await CloudOnlineArtistAlbumInfo.CreateAsync(
                            albumsElement[i]!,
                            _api,
                            false
                        );
                        albumInfos[i] = albumInfo;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            );
            foreach (var album in albumInfos)
            {
                if (album is not null && album.IsAvailable)
                {
                    songs.AddRange(album.SongList);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return songs;
    }
}
