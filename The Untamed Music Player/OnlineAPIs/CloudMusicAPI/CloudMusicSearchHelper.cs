using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public class CloudMusicSearchHelper
{
    private static readonly SemaphoreSlim _searchSemaphore = new(1, 1);

    private static NeteaseCloudMusicApi _api { get; set; } = new();

    public static async Task SearchAsync(string keyWords, CloudBriefOnlineMusicInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.SearchedSongIDs.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await _api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
                { "offset", "0" }
            });
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取songCount
            if (root.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("songCount", out var songCountElement))
            {
                list.SongCount = songCountElement.GetInt32();

                if (list.SongCount == 0)
                {
                    list.HasAllLoaded = true;
                    return;
                }

                // 获取songs数组
                if (resultElement.TryGetProperty("songs", out var songsElement))
                {
                    await ProcessSongsAsync(songsElement, list);
                    list.Page = 1;
                }
                else
                {
                    throw new Exception("获取歌曲列表失败");
                }
            }
            else
            {
                throw new Exception("获取歌曲数量失败");
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

    public static async Task SearchMoreAsync(CloudBriefOnlineMusicInfoList list)
    {
        await _searchSemaphore.WaitAsync();
        try
        {
            var (_, result) = await _api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
            {
                { "keywords", list.KeyWords },
                { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
                { "offset", (list.Page * 30).ToString() }
            });
            using var document = JsonDocument.Parse(result.ToJsonString());
            var root = document.RootElement;

            // 获取songs数组
            if (root.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("songs", out var songsElement))
            {
                await ProcessSongsAsync(songsElement, list);
                list.Page++;
            }
            else
            {
                throw new Exception("获取歌曲列表失败");
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

    private static async Task ProcessSongsAsync(JsonElement songsElement, CloudBriefOnlineMusicInfoList list)
    {
        var actualCount = songsElement.GetArrayLength();
        var infos = new CloudBriefOnlineMusicInfo[actualCount];

        // 使用 Parallel.ForEachAsync 限制并发数为 8
        await Parallel.ForEachAsync(
            Enumerable.Range(0, actualCount),
            new ParallelOptions(),
            async (i, cancellationToken) =>
            {
                try
                {
                    var info = await CloudBriefOnlineMusicInfo.CreateAsync(songsElement[i]!, _api);
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
            });

        foreach (var info in infos)
        {
            list.Add(info);
        }
    }

    public static async Task<List<SearchResult>> GetSearchResultAsync(string keyWords)
    {
        var list = new List<SearchResult>();
        await Task.Run(async () =>
        {
            try
            {
                var (isOk, result) = await _api.RequestAsync(CloudMusicApiProviders.SearchSuggest, new Dictionary<string, string> { { "keywords", $"{keyWords}" } });

                using var document = JsonDocument.Parse(result.ToJsonString());
                var root = document.RootElement;

                if (root.TryGetProperty("result", out var resultElement))
                {
                    AddResultsFromProperty(resultElement, "songs", 5, "\uE8D6", list);
                    AddResultsFromProperty(resultElement, "albums", 3, "\uE93C", list);
                    AddResultsFromProperty(resultElement, "artists", 3, "\uE77B", list);
                    AddResultsFromProperty(resultElement, "playlists", 2, "\uE728", list);
                }
            }
            catch
            {
                Debug.WriteLine("获取网易云音乐搜索建议失败");
            }
        });
        return list;
    }

    private static void AddResultsFromProperty(JsonElement element, string propertyName, int limit, string icon, List<SearchResult> list)
    {
        if (!element.TryGetProperty(propertyName, out var arrayElement))
        {
            return;
        }

        var names = arrayElement.EnumerateArray()
            .Select(item => item.GetProperty("name").GetString()!)
            .Distinct()
            .Take(limit);

        foreach (var name in names)
        {
            list.Add(new SearchResult
            {
                Icon = icon,
                Label = name
            });
        }
    }
}
