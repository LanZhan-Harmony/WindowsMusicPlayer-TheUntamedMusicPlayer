using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Models;
using ZLogger;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudPlaylistSearchHelper
{
    private static readonly ILogger _logger =
        CoreLoggingService.CreateLogger<CloudPlaylistSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    public static async Task SearchPlaylistsAsync(
        string keyWords,
        CloudOnlinePlaylistInfoList list,
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
            list.SearchedPlaylistIDs.Clear();
            list.KeyWords = keyWords;

            var (playlists, playlistCount) = await SearchInternalAsync(keyWords, 0, api);
            list.PlaylistCount = playlistCount;

            if (playlistCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessPlaylistsAsync(playlists, list);
            list.Page = 1;

            while (list.Count < CloudOnlinePlaylistInfoList.Limit && !list.HasAllLoaded)
            {
                var (morePlaylists, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlinePlaylistInfoList.Limit,
                    api
                );
                if (morePlaylists.Count > 0)
                {
                    await ProcessPlaylistsAsync(morePlaylists, list);
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

    public static async Task SearchMorePlaylistsAsync(
        CloudOnlinePlaylistInfoList list,
        CloudMusicApiService api
    )
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (playlists, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlinePlaylistInfoList.Limit,
                api
            );
            await ProcessPlaylistsAsync(playlists, list);
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
        List<CloudSearchPlaylistDto> Playlists,
        int PlaylistCount
    )> SearchInternalAsync(string keyWords, int offset, CloudMusicApiService api)
    {
        var (_, response) = await api.SearchPlaylistsAsync(
            keyWords,
            CloudOnlinePlaylistInfoList.Limit,
            offset
        );

        if (response?.Result is not { } resultElement)
        {
            throw new Exception("获取搜索结果失败");
        }

        var playlistCount = resultElement.PlaylistCount;
        if (resultElement.Playlists is null)
        {
            if (playlistCount == 0)
            {
                return ([], 0);
            }

            throw new Exception("获取歌单列表失败");
        }

        return (resultElement.Playlists, playlistCount);
    }

    private static async Task ProcessPlaylistsAsync(
        List<CloudSearchPlaylistDto> playlists,
        CloudOnlinePlaylistInfoList list
    )
    {
        var actualCount = playlists.Count;
        if (actualCount == 0)
        {
            return;
        }

        var infos = new BriefCloudOnlinePlaylistInfo[actualCount];
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
                    infos[i] = await CloudMusicModelFactory.CreateBriefPlaylistAsync(playlists[i]);
                }
                catch (Exception ex)
                {
                    lock (list)
                    {
                        list.ListCount++;
                    }
                    _logger.ZLogInformation(ex, $"处理网易云歌单信息失败");
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
