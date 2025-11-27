using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudArtistSearchHelper
{
    private static readonly ILogger _logger =
        LoggingService.CreateLogger<CloudArtistSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchArtistsAsync(string keyWords, CloudOnlineArtistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.SearchedArtistIDs.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", keyWords },
                    { "type", "100" },
                    { "limit", $"{CloudOnlineArtistInfoList.Limit}" },
                    { "offset", "0" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取artistCount
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("artistCount", out var artistCountElement)
            )
            {
                list.ArtistCount = artistCountElement.GetInt32();

                if (list.ArtistCount == 0)
                {
                    list.HasAllLoaded = true;
                    return;
                }

                // 获取artists数组
                if (resultElement.TryGetProperty("artists", out var artistsElement))
                {
                    await ProcessArtistsAsync(artistsElement, list);
                    list.Page = 1;
                }
                else
                {
                    throw new Exception("获取艺术家列表失败");
                }
            }
            else
            {
                throw new Exception("获取艺术家数量失败");
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

    public static async Task SearchMoreArtistsAsync(CloudOnlineArtistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", list.KeyWords },
                    { "type", "100" },
                    { "limit", $"{CloudOnlineArtistInfoList.Limit}" },
                    { "offset", $"{list.Page * CloudOnlineArtistInfoList.Limit}" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取artists数组
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("artists", out var artistsElement)
            )
            {
                await ProcessArtistsAsync(artistsElement, list);
                list.Page++;
            }
            else
            {
                throw new Exception("获取艺术家列表失败");
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

    private static async Task ProcessArtistsAsync(
        JsonElement artistsElement,
        CloudOnlineArtistInfoList list
    )
    {
        var actualCount = artistsElement.GetArrayLength();
        var infos = new BriefCloudOnlineArtistInfo[actualCount];
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
                    var info = await BriefCloudOnlineArtistInfo.CreateAsync(artistsElement[i]!);
                    infos[i] = info;
                }
                catch (Exception ex)
                {
                    lock (list)
                    {
                        list.ListCount++;
                    }
                    _logger.ZLogInformation(ex, $"处理网易云艺术家失败");
                }
            }
        );

        foreach (var info in infos)
        {
            list.Add(info);
        }
    }
}
