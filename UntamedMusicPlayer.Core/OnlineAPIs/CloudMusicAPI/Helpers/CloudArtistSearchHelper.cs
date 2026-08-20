using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudArtistSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudArtistSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    public static async Task SearchArtistsAsync(
        string keyWords,
        CloudOnlineArtistInfoList list,
        CloudMusicApiService api
    )
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

            var (artists, artistCount) = await SearchInternalAsync(keyWords, 0, api);
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
                    list.Page * CloudOnlineArtistInfoList.Limit,
                    api
                );
                if (moreArtists.Count > 0)
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

    public static async Task SearchMoreArtistsAsync(
        CloudOnlineArtistInfoList list,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (artists, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineArtistInfoList.Limit,
                api
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

    private static async Task<(
        List<CloudSearchArtistDto> Artists,
        int ArtistCount
    )> SearchInternalAsync(string keyWords, int offset, CloudMusicApiService api)
    {
        var (_, response) = await api.SearchArtistsAsync(
            keyWords,
            CloudOnlineArtistInfoList.Limit,
            offset
        );

        if (response?.Result is not { } resultElement)
        {
            throw new Exception("获取搜索结果失败");
        }

        var artistCount = resultElement.ArtistCount;
        if (resultElement.Artists is null)
        {
            if (artistCount == 0)
            {
                return ([], 0);
            }

            throw new Exception("获取艺术家列表失败");
        }

        return (resultElement.Artists, artistCount);
    }

    private static async Task ProcessArtistsAsync(
        List<CloudSearchArtistDto> artists,
        CloudOnlineArtistInfoList list
    )
    {
        var actualCount = artists.Count;
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
                    infos[i] = await CloudMusicModelFactory.CreateBriefArtistAsync(artists[i]);
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
            if (info is not null)
            {
                list.Add(info);
            }
        }
    }
}
