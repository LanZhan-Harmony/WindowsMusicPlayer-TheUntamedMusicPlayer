using System.Diagnostics;
using Newtonsoft.Json.Linq;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public class CloudMusicSearchHelper
{
    public static NeteaseCloudMusicApi Api { get; set; } = new();

    public static async Task SearchSongsAsync(string keyWords, CloudBriefOnlineMusicInfoList list)
    {
        list.Page = 0;
        list.ListCount = 0;
        list.HasAllLoaded = false;
        list.Clear();
        list.KeyWords = keyWords;
        var (isOk, result) = await Api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
        {
            { "keywords", keyWords },
            { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
            { "offset", "0" }
        });
        if (!isOk)
        {
            throw new Exception();
        }
        try
        {
            list.SongCount = (int)result["result"]!["songCount"]!;
            if (list.SongCount == 0)
            {
                list.HasAllLoaded = true;
                return;
            }
            await ProcessSongsAsync(result["result"]!["songs"]!, list);
            list.Page = 1;
        }
        catch
        {
            throw new Exception("搜索失败");
        }
    }

    public static async Task SearchMoreSongsAsync(CloudBriefOnlineMusicInfoList list)
    {
        var (isOk, result) = await Api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
        {
            { "keywords", list.KeyWords },
            { "limit", CloudBriefOnlineMusicInfoList.Limit.ToString() },
            { "offset", (list.Page * 30).ToString() }
        });
        if (!isOk)
        {
            throw new Exception();
        }
        try
        {
            await ProcessSongsAsync(result["result"]!["songs"]!, list);
            list.Page++;
        }
        catch
        {
            throw new Exception("搜索失败");
        }
    }

    private static async Task ProcessSongsAsync(JToken songs, CloudBriefOnlineMusicInfoList list)
    {
        var actualCount = songs.Count();
        var infos = new CloudBriefOnlineMusicInfo[actualCount];

        // 使用 Parallel.ForEachAsync 限制并发数为 8
        await Parallel.ForEachAsync(
            Enumerable.Range(0, actualCount),
            new ParallelOptions(),
            async (i, cancellationToken) =>
            {
                try
                {
                    var info = await CloudBriefOnlineMusicInfo.CreateAsync(songs[i]!, Api);
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
                var songs = result["result"]!["songs"]?
                    .Select(t => (string)t["name"]!)
                    .Distinct() ?? [];
                var albums = result["result"]!["albums"]?
                    .Select(t => (string)t["name"]!)
                    .Distinct() ?? [];
                var artists = result["result"]!["artists"]?
                    .Select(t => (string)t["name"]!)
                    .Distinct() ?? [];
                var playlists = result["result"]!["playlists"]?
                    .Select(t => (string)t["name"]!)
                    .Distinct() ?? [];
                AddResults(songs, 5, "\uE8D6", list);
                AddResults(albums, 3, "\uE93C", list);
                AddResults(artists, 3, "\uE77B", list);
                AddResults(playlists, 2, "\uE728", list);
            }
        });
        return list;
    }

    private static void AddResults(IEnumerable<string> items, int limit, string icon, List<SearchResult> list)
    {
        foreach (var item in items.Take(limit))
        {
            list.Add(new SearchResult { Icon = icon, Label = item });
        }
    }
}
