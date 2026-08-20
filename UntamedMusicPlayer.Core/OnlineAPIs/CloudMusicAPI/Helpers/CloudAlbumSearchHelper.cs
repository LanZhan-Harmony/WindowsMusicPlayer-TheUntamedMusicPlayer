using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudAlbumSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudAlbumSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    public static async Task SearchAlbumsAsync(
        string keyWords,
        CloudOnlineAlbumInfoList list,
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
            list.SearchedAlbumIDs.Clear();
            list.KeyWords = keyWords;

            var (albums, albumCount) = await SearchInternalAsync(keyWords, 0, api);
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
                    list.Page * CloudOnlineAlbumInfoList.Limit,
                    api
                );
                if (moreAlbums.Count > 0)
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

    public static async Task SearchMoreAlbumsAsync(
        CloudOnlineAlbumInfoList list,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (albums, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlineAlbumInfoList.Limit,
                api
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

    private static async Task<(
        List<CloudSearchAlbumDto> Albums,
        int AlbumCount
    )> SearchInternalAsync(string keyWords, int offset, CloudMusicApiService api)
    {
        var (_, response) = await api.SearchAlbumsAsync(
            keyWords,
            CloudOnlineAlbumInfoList.Limit,
            offset
        );

        if (response?.Result is not { } resultElement)
        {
            throw new Exception("获取搜索结果失败");
        }

        var albumCount = resultElement.AlbumCount;
        if (resultElement.Albums is null)
        {
            if (albumCount == 0)
            {
                return ([], 0);
            }
            throw new Exception("获取专辑列表失败");
        }

        return (resultElement.Albums, albumCount);
    }

    private static async Task ProcessAlbumsAsync(
        List<CloudSearchAlbumDto> albums,
        CloudOnlineAlbumInfoList list
    )
    {
        var actualCount = albums.Count;
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
                    infos[i] = await CloudMusicModelFactory.CreateBriefAlbumAsync(albums[i]);
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
            if (info is not null)
            {
                list.Add(info);
            }
        }
    }
}
