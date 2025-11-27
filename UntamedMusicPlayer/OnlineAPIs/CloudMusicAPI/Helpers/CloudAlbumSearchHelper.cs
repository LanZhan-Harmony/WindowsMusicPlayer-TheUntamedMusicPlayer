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
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.SearchedAlbumIDs.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", keyWords },
                    { "type", "10" },
                    { "limit", $"{CloudOnlineAlbumInfoList.Limit}" },
                    { "offset", "0" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取albumCount
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("albumCount", out var albumCountElement)
            )
            {
                list.AlbumCount = albumCountElement.GetInt32();

                if (list.AlbumCount == 0)
                {
                    list.HasAllLoaded = true;
                    return;
                }

                // 获取albums数组
                if (resultElement.TryGetProperty("albums", out var albumsElement))
                {
                    await ProcessAlbumsAsync(albumsElement, list);
                    list.Page = 1;
                }
                else
                {
                    throw new Exception("获取专辑列表失败");
                }
            }
            else
            {
                throw new Exception("获取专辑数量失败");
            }
        }
        catch
        {
            throw new Exception("搜索失败");
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
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", list.KeyWords },
                    { "type", "10" },
                    { "limit", $"{CloudOnlineAlbumInfoList.Limit}" },
                    { "offset", $"{list.Page * CloudOnlineAlbumInfoList.Limit}" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取albums数组
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("albums", out var albumsElement)
            )
            {
                await ProcessAlbumsAsync(albumsElement, list);
                list.Page++;
            }
            else
            {
                throw new Exception("获取专辑列表失败");
            }
        }
        catch
        {
            throw new Exception("搜索更多失败");
        }
        finally
        {
            _searchSemaphore.Release();
        }
    }

    private static async Task ProcessAlbumsAsync(
        JsonElement albumsElement,
        CloudOnlineAlbumInfoList list
    )
    {
        var actualCount = albumsElement.GetArrayLength();
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
                    var info = await BriefCloudOnlineAlbumInfo.CreateAsync(albumsElement[i]!);
                    infos[i] = info;
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
