using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public class CloudMusicSearchHelper
{
    public static NeteaseCloudMusicApi Api { get; set; } = new();

    public static async Task SearchAsync(string keyWords, CloudBriefOnlineMusicInfoList list)
    {
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.KeyWords = keyWords;

        try
        {
            var (_, result) = await Api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
            {
                { "keywords", keyWords },
                { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
                { "offset", "0" }
            });
            var jsonString = result.ToJsonString();
            using var document = JsonDocument.Parse(jsonString);
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
                if (resultElement.TryGetProperty("songs", out var songsElement) &&
                    songsElement.ValueKind == JsonValueKind.Array)
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
    }

    public static async Task SearchMoreAsync(CloudBriefOnlineMusicInfoList list)
    {
        try
        {
            var (_, result) = await Api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
            {
                { "keywords", list.KeyWords },
                { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
                { "offset", (list.Page * 30).ToString() }
            });

            var jsonString = result.ToJsonString();
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            // 获取songs数组
            if (root.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("songs", out var songsElement) &&
                songsElement.ValueKind == JsonValueKind.Array)
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
                    var info = await CloudBriefOnlineMusicInfo.CreateAsync(songsElement[i]!, Api);
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
            var (isOk, result) = await Api.RequestAsync(CloudMusicApiProviders.SearchSuggest, new Dictionary<string, string> { { "keywords", $"{keyWords}" } });
            if (!isOk)
            {
                Debug.WriteLine("获取网易云音乐搜索建议失败");
            }
            else
            {
                var songs = result["result"]?["songs"] is JsonArray songsArray
                ? songsArray.Select(node => JsonDocument.Parse(node!.ToJsonString()).RootElement.GetProperty("name").GetString()!).Distinct().ToList()
                : [];
                var albums = result["result"]?["albums"] is JsonArray albumsArray
                ? albumsArray.Select(node => JsonDocument.Parse(node!.ToJsonString()).RootElement.GetProperty("name").GetString()!).Distinct().ToList()
                : [];
                var artists = result["result"]?["artists"] is JsonArray artistsArray
                ? artistsArray.Select(node => JsonDocument.Parse(node!.ToJsonString()).RootElement.GetProperty("name").GetString()!).Distinct().ToList()
                : [];
                var playlists = result["result"]?["playlists"] is JsonArray playlistsArray
                ? playlistsArray.Select(node => JsonDocument.Parse(node!.ToJsonString()).RootElement.GetProperty("name").GetString()!).Distinct().ToList()
                : [];
                AddResults(songs, 5, "\uE8D6", list);
                AddResults(albums, 3, "\uE93C", list);
                AddResults(artists, 3, "\uE77B", list);
                AddResults(playlists, 2, "\uE728", list);
            }
        });
        return list;
    }

    private static void AddResults(List<string> items, int limit, string icon, List<SearchResult> list)
    {
        foreach (var item in items.Take(limit))
        {
            list.Add(new SearchResult { Icon = icon, Label = item });
        }
    }
}
