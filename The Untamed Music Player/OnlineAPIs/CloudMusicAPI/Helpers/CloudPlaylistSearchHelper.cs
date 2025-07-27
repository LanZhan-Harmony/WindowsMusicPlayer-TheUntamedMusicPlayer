using System.Diagnostics;
using System.Text.Json;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudPlaylistSearchHelper
{
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    private static readonly NeteaseCloudMusicApi _api = NeteaseCloudMusicApi.Instance;

    public static async Task SearchPlaylistsAsync(string keyWords, CloudOnlinePlaylistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.SearchedPlaylistIDs.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", keyWords },
                    { "type", "1000" },
                    { "limit", $"{CloudOnlinePlaylistInfoList.Limit}" },
                    { "offset", "0" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取PlaylistCount
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("playlistCount", out var playlistCountElement)
            )
            {
                list.PlaylistCount = playlistCountElement.GetInt32();

                if (list.PlaylistCount == 0)
                {
                    list.HasAllLoaded = true;
                    return;
                }

                // 获取Playlists数组
                if (resultElement.TryGetProperty("playlists", out var playlistsElement))
                {
                    await ProcessPlaylistsAsync(playlistsElement, list);
                    list.Page = 1;
                }
                else
                {
                    throw new Exception("获取歌单列表失败");
                }
            }
            else
            {
                throw new Exception("获取歌单数量失败");
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

    public static async Task SearchMorePlaylistsAsync(CloudOnlinePlaylistInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (_, result) = await _api.RequestAsync(
                CloudMusicApiProviders.Search,
                new Dictionary<string, string>
                {
                    { "keywords", list.KeyWords },
                    { "type", "1000" },
                    { "limit", $"{CloudOnlinePlaylistInfoList.Limit}" },
                    { "offset", $"{list.Page * CloudOnlinePlaylistInfoList.Limit}" },
                }
            );
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取Playlists数组
            if (
                root.TryGetProperty("result", out var resultElement)
                && resultElement.TryGetProperty("playlists", out var playlistsElement)
            )
            {
                await ProcessPlaylistsAsync(playlistsElement, list);
                list.Page++;
            }
            else
            {
                throw new Exception("获取歌单列表失败");
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

    private static async Task ProcessPlaylistsAsync(
        JsonElement playlistsElement,
        CloudOnlinePlaylistInfoList list
    )
    {
        var actualCount = playlistsElement.GetArrayLength();
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
                    var info = await BriefCloudOnlinePlaylistInfo.CreateAsync(playlistsElement[i]!);
                    infos[i] = info;
                }
                catch (Exception ex)
                {
                    lock (list)
                    {
                        list.ListCount++;
                    }
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        );

        foreach (var info in infos)
        {
            list.Add(info);
        }
    }
}
