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
        try
        {
            list.Page = 0;
            list.ListCount = 0;
            list.HasAllLoaded = false;
            list.Clear();
            list.SearchedArtistIDs.Clear();
            list.KeyWords = keyWords;

            var (artists, artistCount) = await SearchInternalAsync(keyWords, 0);
            list.ArtistCount = artistCount;

            if (artistCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessArtistsAsync(artists, list);
            list.Page = 1;

            while (list.Count < CloudOnlineArtistInfoList.Limit && !list.HasAllLoaded)
            {
                var (moreArtists, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlineArtistInfoList.Limit
                );
                if (moreArtists.GetArrayLength() > 0)
                {
                    await ProcessArtistsAsync(moreArtists, list);
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

    public static async Task SearchMoreArtistsAsync(CloudOnlineArtistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (artists, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineArtistInfoList.Limit
            );
            await ProcessArtistsAsync(artists, list);
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

    private static async Task<(JsonElement Artists, int ArtistCount)> SearchInternalAsync(
        string keyWords,
        int offset
    )
    {
        var (_, result) = await _api.RequestAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "type", "100" },
                { "limit", $"{CloudOnlineArtistInfoList.Limit}" },
                { "offset", $"{offset}" },
            }
        );

        using var document = JsonDocument.Parse(result.ToJsonString());
        var root = document.RootElement;

        if (!root.TryGetProperty("result", out var resultElement))
        {
            throw new Exception("获取搜索结果失败");
        }

        resultElement.TryGetProperty("artistCount", out var artistCountElement);
        var artistCount =
            artistCountElement.ValueKind == JsonValueKind.Number
                ? artistCountElement.GetInt32()
                : 0;

        if (!resultElement.TryGetProperty("artists", out var artistsElement))
        {
            if (artistCount == 0)
            {
                return (default, 0);
            }

            throw new Exception("获取艺术家列表失败");
        }

        return (artistsElement.Clone(), artistCount);
    }

    private static async Task ProcessArtistsAsync(
        JsonElement artistsElement,
        CloudOnlineArtistInfoList list
    )
    {
        var actualCount =
            artistsElement.ValueKind == JsonValueKind.Array ? artistsElement.GetArrayLength() : 0;
        if (actualCount == 0)
        {
            return;
        }

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
                    infos[i] = await BriefCloudOnlineArtistInfo.CreateAsync(artistsElement[i]);
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
