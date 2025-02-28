using System.Diagnostics;
using Newtonsoft.Json.Linq;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
public partial class CloudBriefOnlineMusicInfoList : IBriefOnlineMusicInfoList
{
    private readonly NeteaseCloudMusicApi _api = new();
    private const byte _limit = 30;
    private ushort _page = 0;
    private int _songCount = 0;
    private int _listCount = 0;

    public CloudBriefOnlineMusicInfoList() { }

    public async override Task SearchAsync(string keyWords)
    {
        _page = 0;
        _listCount = 0;
        HasAllLoaded = false;
        Clear();
        _keyWords = keyWords;
        var (isOk, result) = await _api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
        {
            { "keywords", keyWords },
            { "limit", _limit.ToString() },
            { "offset", "0" }
        });
        if (!isOk)
        {
            throw new Exception();
        }
        try
        {
            _songCount = (int)result["result"]!["songCount"]!;
            if (_songCount == 0)
            {
                HasAllLoaded = true;
                return;
            }
            await ProcessSongsAsync(result["result"]!["songs"]!);
            _page = 1;
        }
        catch
        {
            throw new Exception("搜索失败");
        }
    }

    public async override Task SearchMoreAsync()
    {
        var (isOk, result) = await _api.RequestAsync(CloudMusicApiProviders.Search, new Dictionary<string, string>
        {
            { "keywords", _keyWords },
            { "limit", _limit.ToString() },
            { "offset", (_page*30).ToString() }
        });
        if (!isOk)
        {
            throw new Exception();
        }
        try
        {
            await ProcessSongsAsync(result["result"]!["songs"]!);
            _page++;
        }
        catch
        {
            throw new Exception("搜索失败");
        }
    }

    private async Task ProcessSongsAsync(JToken songs)
    {
        var actualCount = songs.Count();
        var infos = new CloudBriefOnlineMusicInfo[actualCount];
        var groupTasks = new List<Task>();

        // 每组 8 首歌曲
        for (var i = 0; i < actualCount; i += 8)
        {
            var start = i;
            var end = Math.Min(i + 8, actualCount);
            // 使用 Task.Run 将每组放在一个线程中执行
            groupTasks.Add(Task.Run(async () =>
            {
                for (var j = start; j < end; j++)
                {
                    try
                    {
                        var info = await CloudBriefOnlineMusicInfo.CreateAsync(songs[j]!, _api);
                        infos[j] = info;
                    }
                    catch (Exception ex)
                    {
                        _listCount++;
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }));
        }
        await Task.WhenAll(groupTasks);

        foreach (var info in infos)
        {
            Add(info);
        }
    }

    protected new void Add(IBriefOnlineMusicInfo? info)
    {
        _listCount++;
        if (info is not null && info.IsAvailable)
        {
            base.Add(info);
        }
        if (_listCount == _songCount)
        {
            HasAllLoaded = true;
        }
    }

    public async override Task<List<SearchResult>> GetSearchResultAsync(string keyWords)
    {
        var list = new List<SearchResult>();
        await Task.Run(async () =>
        {
            var (isOk, result) = await _api.RequestAsync(CloudMusicApiProviders.SearchSuggest, new Dictionary<string, string> { { "keywords", $"{keyWords}" } });
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