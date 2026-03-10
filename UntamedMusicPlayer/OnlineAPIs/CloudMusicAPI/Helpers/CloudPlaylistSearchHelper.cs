using System.Text.Json;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.Services;
using ZLogger;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Helpers;

public sealed class CloudPlaylistSearchHelper
{
    private static readonly ILogger _logger =
        LoggingService.CreateLogger<CloudPlaylistSearchHelper>();
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);
    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchPlaylistsAsync(string keyWords, CloudOnlinePlaylistInfoList list)
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

            var (playlists, playlistCount) = await SearchInternalAsync(keyWords, 0);
            list.PlaylistCount = playlistCount;

            if (playlistCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }

            await ProcessPlaylistsAsync(playlists, list);
            list.Page = 1;

            // 如果加载后的数量没达到Limit且还有更多，则继续加载更多
            while (list.Count < CloudOnlinePlaylistInfoList.Limit && !list.HasAllLoaded)
            {
                var (morePlaylists, _) = await SearchInternalAsync(
                    list.KeyWords,
                    list.Page * CloudOnlinePlaylistInfoList.Limit
                );
                if (morePlaylists.GetArrayLength() > 0)
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

    public static async Task SearchMorePlaylistsAsync(CloudOnlinePlaylistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (playlists, _) = await SearchInternalAsync(
                list.KeyWords,
                list.Page * CloudOnlinePlaylistInfoList.Limit
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

    private static async Task<(JsonElement Playlists, int PlaylistCount)> SearchInternalAsync(
        string keyWords,
        int offset
    )
    {
        var (_, result) = await _api.RequestAsync(
            CloudMusicApiProviders.Search,
            new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "type", "1000" },
                { "limit", $"{CloudOnlinePlaylistInfoList.Limit}" },
                { "offset", $"{offset}" },
            }
        );

        using var document = JsonDocument.Parse(result.ToJsonString());
        var root = document.RootElement;

        if (!root.TryGetProperty("result", out var resultElement))
        {
            throw new Exception("获取搜索结果失败");
        }

        resultElement.TryGetProperty("playlistCount", out var playlistCountElement);
        var playlistCount =
            playlistCountElement.ValueKind == JsonValueKind.Number
                ? playlistCountElement.GetInt32()
                : 0;

        if (!resultElement.TryGetProperty("playlists", out var playlistsElement))
        {
            if (playlistCount == 0)
            {
                return (default, 0);
            }

            throw new Exception("获取歌单列表失败");
        }

        return (playlistsElement.Clone(), playlistCount);
    }

    private static async Task ProcessPlaylistsAsync(
        JsonElement playlistsElement,
        CloudOnlinePlaylistInfoList list
    )
    {
        var actualCount =
            playlistsElement.ValueKind == JsonValueKind.Array
                ? playlistsElement.GetArrayLength()
                : 0;
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
                    infos[i] = await BriefCloudOnlinePlaylistInfo.CreateAsync(playlistsElement[i]);
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
            list.Add(info);
        }
    }
}
