using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.OnlineAPI.BilibiliMusicAPI.Extensions;

namespace UntamedMusicPlayer.OnlineAPI.BilibiliMusicAPI;

/// <summary>
/// bilibili 音乐 API
/// </summary>
public sealed partial class BilibiliMusicApiService : IDisposable
{
    private const int PageSize = 20;

    private static readonly QueryCollection CommonHeaders =
    [
        new(
            "user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36 Edg/89.0.774.63"
        ),
        new("accept", "*/*"),
        new("accept-encoding", "gzip, deflate, br"),
        new("accept-language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"),
    ];

    private static readonly QueryCollection SearchHeaders =
    [
        new(
            "user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36 Edg/89.0.774.63"
        ),
        new("accept", "application/json, text/plain, */*"),
        new("accept-encoding", "gzip, deflate, br"),
        new("origin", "https://search.bilibili.com"),
        new("sec-fetch-site", "same-site"),
        new("sec-fetch-mode", "cors"),
        new("sec-fetch-dest", "empty"),
        new("referer", "https://search.bilibili.com/"),
        new("accept-language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"),
    ];

    [GeneratedRegex("(<em(.*?)>)|(</em>)", RegexOptions.Compiled)]
    private static partial Regex EmTagRegex();

    [GeneratedRegex("《(.+?)》", RegexOptions.Compiled)]
    private static partial Regex AliasRegex();

    [GeneratedRegex("[!'()*]", RegexOptions.Compiled)]
    private static partial Regex RidFilterRegex();

    [GeneratedRegex("^\\s*(\\d+)\\s*$", RegexOptions.Compiled)]
    private static partial Regex NumericIdRegex();

    [GeneratedRegex("^(?:.*)fid=(\\d+).*$", RegexOptions.Compiled)]
    private static partial Regex FidIdRegex();

    [GeneratedRegex("/playlist/pl(\\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PlaylistIdRegex();

    [GeneratedRegex("/list/ml(\\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ListIdRegex();

    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;

    private string? _buvid3;
    private string? _buvid4;
    private string? _imgKey;
    private string? _subKey;
    private DateTime _wbiSyncDate;

    public BilibiliMusicApiService()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
        };
        _client = new HttpClient(_clientHandler);
    }

    public Task<JsonObject> SearchMusicAsync(string keyword, int page) =>
        SearchAlbumAsync(keyword, page);

    public async Task<JsonObject> SearchAlbumAsync(string keyword, int page)
    {
        var data = await SearchBaseAsync(keyword, page, "video");
        var result = new JsonArray();

        foreach (var item in data["result"]?.AsArray() ?? [])
        {
            if (item is JsonObject source)
            {
                result.Add(FormatMedia(source));
            }
        }

        var numResults = data["numResults"]?.GetValue<int>() ?? 0;
        return new JsonObject { ["isEnd"] = numResults <= page * PageSize, ["data"] = result };
    }

    public async Task<JsonObject> SearchArtistAsync(string keyword, int page)
    {
        var data = await SearchBaseAsync(keyword, page, "bili_user");
        var result = new JsonArray();

        foreach (var item in data["result"]?.AsArray() ?? [])
        {
            if (item is not JsonObject source)
            {
                continue;
            }

            var upic = GetString(source, "upic");
            result.Add(
                new JsonObject
                {
                    ["name"] = GetString(source, "uname"),
                    ["id"] = GetString(source, "mid"),
                    ["fans"] = GetString(source, "fans"),
                    ["description"] = GetString(source, "usign"),
                    ["avatar"] =
                        upic?.StartsWith("//", StringComparison.Ordinal) == true
                            ? $"https://{upic[2..]}"
                            : upic,
                    ["worksNum"] = GetString(source, "videos"),
                }
            );
        }

        var numResults = data["numResults"]?.GetValue<int>() ?? 0;
        return new JsonObject { ["isEnd"] = numResults <= page * PageSize, ["data"] = result };
    }

    public async Task<JsonObject?> GetMediaSourceAsync(JsonObject musicItem, string quality)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var cid = GetString(musicItem, "cid");
        if (string.IsNullOrWhiteSpace(cid))
        {
            var cidRes = await GetCidAsync(
                GetString(musicItem, "bvid"),
                GetString(musicItem, "aid")
            );
            cid = GetString(cidRes["data"] as JsonObject, "cid");
        }

        if (string.IsNullOrWhiteSpace(cid))
        {
            return null;
        }

        var query = new QueryCollection { { "cid", cid }, { "fnval", "16" } };
        var bvid = GetString(musicItem, "bvid");
        var aid = GetString(musicItem, "aid");
        if (!string.IsNullOrWhiteSpace(bvid))
        {
            query.Add("bvid", bvid);
        }
        else if (!string.IsNullOrWhiteSpace(aid))
        {
            query.Add("aid", aid);
        }

        var res = await GetJsonAsync(
            "https://api.bilibili.com/x/player/playurl",
            query,
            CommonHeaders
        );
        if (res["data"] is not JsonObject data)
        {
            return null;
        }

        string? url = null;
        if (data["dash"] is JsonObject dash && dash["audio"] is JsonArray audios)
        {
            var audioList = audios
                .OfType<JsonObject>()
                .OrderBy(a => a["bandwidth"]?.GetValue<int>() ?? 0)
                .ToList();

            if (audioList.Count > 0)
            {
                var index = quality switch
                {
                    "low" => 0,
                    "standard" => 1,
                    "high" => 2,
                    "super" => 3,
                    _ => 1,
                };
                index = Math.Min(index, audioList.Count - 1);
                url =
                    GetString(audioList[index], "baseUrl")
                    ?? GetString(audioList[index], "base_url");
            }
        }
        else
        {
            url = data["durl"]?.AsArray().FirstOrDefault()?["url"]?.ToString();
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var host = new Uri(url).Host;
        var referValue = $"https://www.bilibili.com/video/{bvid ?? aid ?? string.Empty}";

        return new JsonObject
        {
            ["url"] = url,
            ["headers"] = new JsonObject
            {
                ["user-agent"] = CommonHeaders[0].Value,
                ["accept"] = "*/*",
                ["host"] = host,
                ["accept-encoding"] = "gzip, deflate, br",
                ["connection"] = "keep-alive",
                ["referer"] = referValue,
            },
        };
    }

    public async Task<JsonObject> GetAlbumInfoAsync(JsonObject albumItem)
    {
        ArgumentNullException.ThrowIfNull(albumItem);

        var cidRes = await GetCidAsync(GetString(albumItem, "bvid"), GetString(albumItem, "aid"));
        var data = cidRes["data"] as JsonObject;
        var cid = GetString(data, "cid");
        var pages = data?["pages"]?.AsArray();

        var musicList = new JsonArray();
        if (pages is null || pages.Count <= 1)
        {
            var copy = CloneObject(albumItem);
            if (!string.IsNullOrWhiteSpace(cid))
            {
                copy["cid"] = cid;
            }
            musicList.Add(copy);
        }
        else
        {
            foreach (var page in pages)
            {
                if (page is not JsonObject p)
                {
                    continue;
                }

                var copy = CloneObject(albumItem);
                copy["cid"] = GetString(p, "cid");
                copy["title"] = GetString(p, "part");
                copy["duration"] = DurationToSec(p["duration"]);
                copy["id"] = GetString(p, "cid");

                musicList.Add(copy);
            }
        }

        return new JsonObject { ["musicList"] = musicList };
    }

    public async Task<JsonObject> GetArtistWorksAsync(JsonObject artistItem, int page, string type)
    {
        _ = type;
        await EnsureCookieAsync();

        var id =
            GetString(artistItem, "id")
            ?? throw new ArgumentException("artistItem.id 不能为空", nameof(artistItem));

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var baseParams = new Dictionary<string, object?>
        {
            ["mid"] = id,
            ["ps"] = 30,
            ["tid"] = 0,
            ["pn"] = page,
            ["web_location"] = 1550101,
            ["order_avoided"] = true,
            ["order"] = "pubdate",
            ["keyword"] = string.Empty,
            ["platform"] = "web",
            ["dm_img_list"] = "[]",
            ["dm_img_str"] = "V2ViR0wgMS4wIChPcGVuR0wgRVMgMi4wIENocm9taXVtKQ",
            ["dm_cover_img_str"] =
                "QU5HTEUgKE5WSURJQSwgTlZJRElBIEdlRm9yY2UgR1RYIDE2NTAgKDB4MDAwMDFGOTEpIERpcmVjdDNEMTEgdnNfNV8wIHBzXzVfMCwgRDNEMTEpR29vZ2xlIEluYy4gKE5WSURJQS",
            ["dm_img_inter"] = "{\"ds\":[],\"wh\":[0,0,0],\"of\":[0,0,0]}",
            ["wts"] = now,
        };

        var wRid = await GetRidAsync(baseParams);
        var query = ToQueryCollection(baseParams);
        query.Add("w_rid", wRid);

        var headers = new QueryCollection
        {
            { "user-agent", CommonHeaders[0].Value },
            { "accept", "*/*" },
            { "accept-encoding", "gzip, deflate, br, zstd" },
            { "origin", "https://space.bilibili.com" },
            { "sec-fetch-site", "same-site" },
            { "sec-fetch-mode", "cors" },
            { "sec-fetch-dest", "empty" },
            { "referer", $"https://space.bilibili.com/{id}/video" },
            { "cookie", $"buvid3={_buvid3};buvid4={_buvid4}" },
        };

        var res = await GetJsonAsync(
            "https://api.bilibili.com/x/space/wbi/arc/search",
            query,
            headers
        );
        var resultData = res["data"] as JsonObject ?? [];

        var mapped = new JsonArray();
        foreach (var item in resultData["list"]?["vlist"]?.AsArray() ?? [])
        {
            if (item is JsonObject source)
            {
                mapped.Add(FormatMedia(source));
            }
        }

        var pn = resultData["page"]?["pn"]?.GetValue<int>() ?? 0;
        var ps = resultData["page"]?["ps"]?.GetValue<int>() ?? 0;
        var count = resultData["page"]?["count"]?.GetValue<int>() ?? 0;

        return new JsonObject { ["isEnd"] = pn * ps >= count, ["data"] = mapped };
    }

    public Task<JsonArray> GetTopListsAsync()
    {
        var weekly = new JsonObject { ["title"] = "每周必看", ["data"] = new JsonArray() };
        var precious = new JsonObject
        {
            ["title"] = "入站必刷",
            ["data"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "popular/precious?page_size=100&page=1",
                    ["title"] = "入站必刷",
                    ["coverImg"] =
                        "https://s1.hdslb.com/bfs/static/jinkela/popular/assets/icon_history.png",
                },
            },
        };

        var board = new JsonObject
        {
            ["title"] = "排行榜",
            ["data"] = new JsonArray
            {
                BoardItem("ranking/v2?rid=0&type=all", "全站"),
                BoardItem("ranking/v2?rid=3&type=all", "音乐"),
                BoardItem("ranking/v2?rid=1&type=all", "动画"),
                BoardItem("ranking/v2?rid=119&type=all", "鬼畜"),
                BoardItem("ranking/v2?rid=168&type=all", "国创相关"),
                BoardItem("ranking/v2?rid=129&type=all", "舞蹈"),
                BoardItem("ranking/v2?rid=4&type=all", "游戏"),
                BoardItem("ranking/v2?rid=36&type=all", "知识"),
                BoardItem("ranking/v2?rid=188&type=all", "科技"),
                BoardItem("ranking/v2?rid=234&type=all", "运动"),
                BoardItem("ranking/v2?rid=223&type=all", "汽车"),
                BoardItem("ranking/v2?rid=160&type=all", "生活"),
                BoardItem("ranking/v2?rid=211&type=all", "美食"),
                BoardItem("ranking/v2?rid=217&type=all", "动物圈"),
                BoardItem("ranking/v2?rid=155&type=all", "时尚"),
                BoardItem("ranking/v2?rid=5&type=all", "娱乐"),
                BoardItem("ranking/v2?rid=181&type=all", "影视"),
                BoardItem("ranking/v2?rid=0&type=origin", "原创"),
                BoardItem("ranking/v2?rid=0&type=rookie", "新人"),
            },
        };

        return BuildTopListsAsync(weekly, precious, board);
    }

    public async Task<JsonObject> GetTopListDetailAsync(JsonObject topListItem)
    {
        ArgumentNullException.ThrowIfNull(topListItem);

        var id =
            GetString(topListItem, "id")
            ?? throw new ArgumentException("topListItem.id 不能为空", nameof(topListItem));

        var headers = new QueryCollection();
        headers.AddRange(CommonHeaders);
        headers.Add("referer", "https://www.bilibili.com/");
        var res = await GetJsonAsync(
            $"https://api.bilibili.com/x/web-interface/{id}",
            null,
            headers
        );

        var musicList = new JsonArray();
        foreach (var item in res["data"]?["list"]?.AsArray() ?? [])
        {
            if (item is JsonObject source)
            {
                musicList.Add(FormatMedia(source));
            }
        }

        var result = CloneObject(topListItem);
        result["musicList"] = musicList;
        return result;
    }

    public async Task<JsonArray?> ImportMusicSheetAsync(string urlLike)
    {
        var id = ExtractSheetId(urlLike);
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var medias = await GetFavoriteListAsync(id);
        var result = new JsonArray();

        foreach (var item in medias)
        {
            result.Add(
                new JsonObject
                {
                    ["id"] = GetString(item, "id"),
                    ["aid"] = GetString(item, "aid"),
                    ["bvid"] = GetString(item, "bvid"),
                    ["artwork"] = GetString(item, "cover"),
                    ["title"] = GetString(item, "title"),
                    ["artist"] = GetString(item["upper"] as JsonObject, "name"),
                    ["album"] = GetString(item, "bvid") ?? GetString(item, "aid"),
                    ["duration"] = DurationToSec(item["duration"]),
                }
            );
        }

        return result;
    }

    public async Task<JsonObject> GetMusicCommentsAsync(JsonObject musicItem)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var aid =
            GetString(musicItem, "aid")
            ?? throw new ArgumentException("musicItem.aid 不能为空", nameof(musicItem));

        var parameters = new Dictionary<string, object?>
        {
            ["type"] = 1,
            ["mode"] = 3,
            ["oid"] = aid,
            ["plat"] = 1,
            ["web_location"] = 1315875,
            ["wts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        var wRid = await GetRidAsync(parameters);

        var query = ToQueryCollection(parameters);
        query.Add("w_rid", wRid);

        var res = await GetJsonAsync("https://api.bilibili.com/x/v2/reply/wbi/main", query, null);
        var replies = res["data"]?["replies"]?.AsArray() ?? [];

        var comments = new JsonArray();
        foreach (var item in replies)
        {
            if (item is not JsonObject comment)
            {
                continue;
            }

            var formatted = FormatComment(comment);
            var nested = comment["replies"]?.AsArray();
            if (nested is not null && nested.Count > 0)
            {
                var sub = new JsonArray();
                foreach (var child in nested.OfType<JsonObject>())
                {
                    sub.Add(FormatComment(child));
                }
                formatted["replies"] = sub;
            }

            comments.Add(formatted);
        }

        return new JsonObject { ["isEnd"] = true, ["data"] = comments };
    }

    private async Task<JsonObject> GetCidAsync(string? bvid, string? aid)
    {
        var query = new QueryCollection();
        if (!string.IsNullOrWhiteSpace(bvid))
        {
            query.Add("bvid", bvid);
        }
        else if (!string.IsNullOrWhiteSpace(aid))
        {
            query.Add("aid", aid);
        }

        return await GetJsonAsync(
            "https://api.bilibili.com/x/web-interface/view?%s",
            query,
            CommonHeaders
        );
    }

    private async Task<JsonObject> SearchBaseAsync(string keyword, int page, string searchType)
    {
        await EnsureCookieAsync();

        var query = new QueryCollection
        {
            { "context", string.Empty },
            { "page", page.ToString(CultureInfo.InvariantCulture) },
            { "order", string.Empty },
            { "page_size", PageSize.ToString(CultureInfo.InvariantCulture) },
            { "keyword", keyword },
            { "duration", string.Empty },
            { "tids_1", string.Empty },
            { "tids_2", string.Empty },
            { "__refresh__", "true" },
            { "_extra", string.Empty },
            { "highlight", "1" },
            { "single_column", "0" },
            { "platform", "pc" },
            { "from_source", string.Empty },
            { "search_type", searchType },
            { "dynamic_offset", "0" },
        };

        var headers = new QueryCollection();
        headers.AddRange(SearchHeaders);
        headers.Add("cookie", $"buvid3={_buvid3};buvid4={_buvid4}");

        var res = await GetJsonAsync(
            "https://api.bilibili.com/x/web-interface/search/type",
            query,
            headers
        );
        return res["data"]?.AsObject() ?? [];
    }

    private async Task EnsureCookieAsync()
    {
        if (!string.IsNullOrWhiteSpace(_buvid3) && !string.IsNullOrWhiteSpace(_buvid4))
        {
            return;
        }

        var headers = new QueryCollection
        {
            {
                "User-Agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1 Edg/114.0.0.0"
            },
        };

        var res = await GetJsonAsync(
            "https://api.bilibili.com/x/frontend/finger/spi",
            null,
            headers
        );
        var data = res["data"] as JsonObject;
        _buvid3 = GetString(data, "b_3");
        _buvid4 = GetString(data, "b_4");
    }

    private async Task<(string img, string sub)> GetWbiKeysAsync()
    {
        if (
            !string.IsNullOrWhiteSpace(_imgKey)
            && !string.IsNullOrWhiteSpace(_subKey)
            && _wbiSyncDate.Date == DateTime.Today
        )
        {
            return (_imgKey, _subKey);
        }

        var data = await GetBiliTicketAsync();
        var nav = data["nav"] as JsonObject ?? [];

        _imgKey = Path.GetFileNameWithoutExtension(GetString(nav, "img") ?? string.Empty);
        _subKey = Path.GetFileNameWithoutExtension(GetString(nav, "sub") ?? string.Empty);
        _wbiSyncDate = DateTime.Now;

        return (_imgKey, _subKey);
    }

    private async Task<JsonObject> GetBiliTicketAsync()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var hexSign = HmacSha256Hex("XgwSnGZ1p", $"ts{ts}");

        var query = new QueryCollection
        {
            { "key_id", "ec02" },
            { "hexsign", hexSign },
            { "context[ts]", ts.ToString(CultureInfo.InvariantCulture) },
            { "csrf", string.Empty },
        };

        var headers = new QueryCollection
        {
            {
                "User-Agent",
                "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/115.0"
            },
        };

        var res = await SendForJsonAsync(
            HttpMethod.Post,
            "https://api.bilibili.com/bapis/bilibili.api.ticket.v1.Ticket/GenWebTicket",
            query,
            headers,
            content: null
        );
        return res["data"]?.AsObject() ?? [];
    }

    private async Task<string> GetRidAsync(Dictionary<string, object?> parameters)
    {
        var (img, sub) = await GetWbiKeysAsync();
        var mixin = GetMixinKey(img + sub);

        var pairs = new List<string>();
        foreach (var key in parameters.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            var value = parameters[key];
            if (value is null)
            {
                continue;
            }

            var text = value.ToString() ?? string.Empty;
            text = RidFilterRegex().Replace(text, string.Empty);
            pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(text)}");
        }

        var f = string.Join("&", pairs);
        return Md5Hex(f + mixin);
    }

    private async Task<List<JsonObject>> GetFavoriteListAsync(string id)
    {
        var result = new List<JsonObject>();
        var page = 1;

        while (true)
        {
            try
            {
                var query = new QueryCollection
                {
                    { "media_id", id },
                    { "platform", "web" },
                    { "ps", "20" },
                    { "pn", page.ToString(CultureInfo.InvariantCulture) },
                };

                var res = await GetJsonAsync(
                    "https://api.bilibili.com/x/v3/fav/resource/list",
                    query,
                    null
                );
                var data = res["data"] as JsonObject;
                var medias = data?["medias"]?.AsArray() ?? [];
                foreach (var media in medias.OfType<JsonObject>())
                {
                    result.Add(media);
                }

                var hasMore = data?["has_more"]?.GetValue<bool>() ?? false;
                if (!hasMore)
                {
                    break;
                }

                page++;
            }
            catch
            {
                break;
            }
        }

        return result;
    }

    private async Task<JsonArray> BuildTopListsAsync(
        JsonObject weekly,
        JsonObject precious,
        JsonObject board
    )
    {
        var weeklyRes = await GetJsonAsync(
            "https://api.bilibili.com/x/web-interface/popular/series/list",
            null,
            new QueryCollection
            {
                {
                    "user-agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"
                },
            }
        );

        var weeklyData = weekly["data"]?.AsArray() ?? [];
        foreach (var item in weeklyRes["data"]?["list"]?.AsArray().Take(8) ?? [])
        {
            if (item is not JsonObject w)
            {
                continue;
            }

            weeklyData.Add(
                new JsonObject
                {
                    ["id"] = $"popular/series/one?number={GetString(w, "number")}",
                    ["title"] = GetString(w, "subject"),
                    ["description"] = GetString(w, "name"),
                    ["coverImg"] =
                        "https://s1.hdslb.com/bfs/static/jinkela/popular/assets/icon_weekly.png",
                }
            );
        }

        return [weekly, precious, board];
    }

    private static JsonObject BoardItem(string id, string title) =>
        new()
        {
            ["id"] = id,
            ["title"] = title,
            ["coverImg"] = "https://s1.hdslb.com/bfs/static/jinkela/popular/assets/icon_rank.png",
        };

    private async Task<JsonObject> GetJsonAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>>? query,
        IEnumerable<KeyValuePair<string, string>>? headers
    ) => await SendForJsonAsync(HttpMethod.Get, url, query, headers, null);

    private async Task<JsonObject> SendForJsonAsync(
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>>? query,
        IEnumerable<KeyValuePair<string, string>>? headers,
        string? content
    )
    {
        using var response = await _client.SendAsync(
            method,
            url,
            query,
            headers,
            (string?)content,
            "application/json"
        );
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(text)?.AsObject() ?? [];
    }

    private static int DurationToSec(JsonNode? duration)
    {
        if (duration is null)
        {
            return 0;
        }

        if (duration is JsonValue number && number.TryGetValue<int>(out var seconds))
        {
            return seconds;
        }

        var text = duration.ToString();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric;
        }

        var parts = text.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var sum = 0;
        foreach (var part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            {
                continue;
            }
            sum = 60 * sum + v;
        }

        return sum;
    }

    private static JsonObject FormatMedia(JsonObject source)
    {
        var rawTitle = GetString(source, "title") ?? string.Empty;
        var title = WebUtility.HtmlDecode(EmTagRegex().Replace(rawTitle, string.Empty));
        var pic = GetString(source, "pic");

        var result = new JsonObject
        {
            ["id"] =
                GetString(source, "cid") ?? GetString(source, "bvid") ?? GetString(source, "aid"),
            ["aid"] = GetString(source, "aid"),
            ["bvid"] = GetString(source, "bvid"),
            ["artist"] =
                GetString(source, "author") ?? GetString(source["owner"] as JsonObject, "name"),
            ["title"] = title,
            ["alias"] = AliasRegex().Match(title) is { Success: true } m ? m.Groups[1].Value : null,
            ["album"] = GetString(source, "bvid") ?? GetString(source, "aid"),
            ["artwork"] =
                pic?.StartsWith("//", StringComparison.Ordinal) == true ? "http:" + pic : pic,
            ["duration"] = DurationToSec(source["duration"]),
            ["tags"] = GetString(source, "tag") is { Length: > 0 } tag
                ? new JsonArray(
                    tag.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => JsonValue.Create(s.Trim()))
                        .ToArray()
                )
                : null,
            ["date"] = FormatDate(GetString(source, "pubdate") ?? GetString(source, "created")),
        };

        return result;
    }

    private static JsonObject FormatComment(JsonObject item)
    {
        var location = GetString(item["reply_control"] as JsonObject, "location");
        if (location?.StartsWith("IP属地：", StringComparison.Ordinal) == true)
        {
            location = location[5..];
        }
        else
        {
            location = null;
        }

        return new JsonObject
        {
            ["id"] = GetString(item, "rpid"),
            ["nickName"] = GetString(item["member"] as JsonObject, "uname"),
            ["avatar"] = GetString(item["member"] as JsonObject, "avatar"),
            ["comment"] = GetString(item["content"] as JsonObject, "message"),
            ["like"] = GetString(item, "like"),
            ["createAt"] = (item["ctime"]?.GetValue<long>() ?? 0) * 1000,
            ["location"] = location,
        };
    }

    private static QueryCollection ToQueryCollection(Dictionary<string, object?> source)
    {
        var query = new QueryCollection(source.Count);
        foreach (var (key, value) in source)
        {
            if (value is null)
            {
                continue;
            }

            query.Add(key, value.ToString() ?? string.Empty);
        }

        return query;
    }

    private static JsonObject CloneObject(JsonObject source)
    {
        var clone = new JsonObject();
        foreach (var (key, value) in source)
        {
            clone[key] = value?.DeepClone();
        }

        return clone;
    }

    private static string GetMixinKey(string text)
    {
        var map = new[]
        {
            46,
            47,
            18,
            2,
            53,
            8,
            23,
            32,
            15,
            50,
            10,
            31,
            58,
            3,
            45,
            35,
            27,
            43,
            5,
            49,
            33,
            9,
            42,
            19,
            29,
            28,
            14,
            39,
            12,
            38,
            41,
            13,
            37,
            48,
            7,
            16,
            24,
            55,
            40,
            61,
            26,
            17,
            0,
            1,
            60,
            51,
            30,
            4,
            22,
            25,
            54,
            21,
            56,
            59,
            6,
            63,
            57,
            62,
            11,
            36,
            20,
            34,
            44,
            52,
        };

        var sb = new StringBuilder();
        foreach (var i in map)
        {
            if (i < text.Length)
            {
                sb.Append(text[i]);
            }
        }

        return sb.Length > 32 ? sb.ToString()[..32] : sb.ToString();
    }

    private static string HmacSha256Hex(string key, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Md5Hex(string text)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? ExtractSheetId(string urlLike)
    {
        if (string.IsNullOrWhiteSpace(urlLike))
        {
            return null;
        }

        var m = NumericIdRegex().Match(urlLike);
        if (m.Success)
        {
            return m.Groups[1].Value;
        }

        m = FidIdRegex().Match(urlLike);
        if (m.Success)
        {
            return m.Groups[1].Value;
        }

        m = PlaylistIdRegex().Match(urlLike);
        if (m.Success)
        {
            return m.Groups[1].Value;
        }

        m = ListIdRegex().Match(urlLike);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? GetString(JsonObject? source, string key)
    {
        if (source is null || !source.TryGetPropertyValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
            JsonValue jsonValue when jsonValue.TryGetValue<int>(out var intValue) =>
                intValue.ToString(CultureInfo.InvariantCulture),
            JsonValue jsonValue when jsonValue.TryGetValue<long>(out var longValue) =>
                longValue.ToString(CultureInfo.InvariantCulture),
            JsonValue jsonValue when jsonValue.TryGetValue<double>(out var doubleValue) =>
                doubleValue.ToString(CultureInfo.InvariantCulture),
            JsonValue jsonValue when jsonValue.TryGetValue<bool>(out var boolValue) =>
                boolValue.ToString(),
            _ => value.ToJsonString(),
        };
    }

    private static string? FormatDate(string? unixSeconds)
    {
        if (
            !long.TryParse(
                unixSeconds,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var ts
            )
        )
        {
            return null;
        }

        return DateTimeOffset
            .FromUnixTimeSeconds(ts)
            .ToLocalTime()
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public void Dispose()
    {
        _client.Dispose();
        _clientHandler.Dispose();
    }
}
