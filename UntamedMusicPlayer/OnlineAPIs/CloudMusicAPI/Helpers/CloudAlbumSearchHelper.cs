using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudAlbumSearchHelper
{
    private static readonly ILogger _logger = LoggingService.CreateLogger<CloudAlbumSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchAlbumsAsync(string keyWords, CloudOnlineAlbumInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            list.Page = 0;
            list.ListCount = 0;
            list.HasAllLoaded = false;
            list.Clear();
            list.SearchedAlbumIDs.Clear();
            list.KeyWords = keyWords;

            var (albums, albumCount) = await SearchInternalAsync(keyWords, 0);
            list.AlbumCount = albumCount;

            if (albumCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessAlbumsAsync(albums, list);
            list.Page = 1;

            while (list.Count < CloudOnlineAlbumInfoList.Limit && !list.HasAllLoaded)
            {
                var (moreAlbums, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlineAlbumInfoList.Limit
                );
                if (moreAlbums.GetArrayLength() > 0)
                {
                    await ProcessAlbumsAsync(moreAlbums, list);
                    list.Page++;
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("搜索失败", ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    public static async Task SearchMoreAlbumsAsync(CloudOnlineAlbumInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (albums, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineAlbumInfoList.Limit
            );
            await ProcessAlbumsAsync(albums, list);
            list.Page++;
        }
        catch (Exception ex)
        {
            throw new Exception("搜索更多失败", ex);
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    private static async Task<(JsonElement Albums, int AlbumCount)> SearchInternalAsync(
        string keyWords,
        int offset
    )
    {
        var (_, result) = await _api.RequestAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "type", "10" },
                { "limit", $"{CloudOnlineAlbumInfoList.Limit}" },
                { "offset", $"{offset}" },
            }
        );

        using var document = JsonDocument.Parse(result.ToJsonString());
        var root = document.RootElement;

        if (!root.TryGetProperty("result", out var resultElement))
        {
            throw new Exception("获取搜索结果失败");
        }

        resultElement.TryGetProperty("albumCount", out var albumCountElement);
        var albumCount =
            albumCountElement.ValueKind == JsonValueKind.Number ? albumCountElement.GetInt32() : 0;

        if (!resultElement.TryGetProperty("albums", out var albumsElement))
        {
            if (albumCount == 0)
            {
                return (default, 0);
            }
            throw new Exception("获取专辑列表失败");
        }

        return (albumsElement.Clone(), albumCount);
    }

    private static async Task ProcessAlbumsAsync(
        JsonElement albumsElement,
        CloudOnlineAlbumInfoList list
    )
    {
        var actualCount =
            albumsElement.ValueKind == JsonValueKind.Array ? albumsElement.GetArrayLength() : 0;
        if (actualCount == 0)
        {
            return;
        }

        var infos = new BriefCloudOnlineAlbumInfo[actualCount];
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
                    infos[i] = await BriefCloudOnlineAlbumInfo.CreateAsync(albumsElement[i]);
                }
                catch (Exception ex)
                {
                    lock (list)
                    {
                        list.ListCount++;
                    }
                    _logger.ZLogInformation(ex, $"处理网易云专辑信息失败");
                }
            }
        );

        foreach (var info in infos)
        {
            list.Add(info);
        }
    }
}
