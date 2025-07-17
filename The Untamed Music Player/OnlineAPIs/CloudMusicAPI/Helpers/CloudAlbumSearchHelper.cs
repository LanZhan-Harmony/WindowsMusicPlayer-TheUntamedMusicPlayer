using System.Diagnostics;
using System.Text.Json;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Helpers;

public class CloudAlbumSearchHelper
{
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    private static readonly NeteaseCloudMusicApi _api = App.GetService<NeteaseCloudMusicApi>();

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
                    { "limit", CloudOnlineAlbumInfoList.Limit.ToString() },
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
                    { "limit", CloudOnlineAlbumInfoList.Limit.ToString() },
                    { "offset", (list.Page * 30).ToString() },
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

        await Parallel.ForEachAsync(
            Enumerable.Range(0, actualCount),
            new ParallelOptions(),
            async (i, cancellationToken) =>
            {
                try
                {
                    var info = await BriefCloudOnlineAlbumInfo.CreateAsync(albumsElement[i]!);
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
