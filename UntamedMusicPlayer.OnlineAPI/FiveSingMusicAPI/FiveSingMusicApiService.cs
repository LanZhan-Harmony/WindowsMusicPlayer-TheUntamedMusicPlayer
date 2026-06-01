using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.OnlineAPI.FiveSingMusicAPI.Extensions;

namespace UntamedMusicPlayer.OnlineAPI.FiveSingMusicAPI;

/// <summary>
/// 5sing 音乐 API
/// </summary>
public sealed partial class FiveSingMusicApiService : IDisposable
{
    private const int PageSize = 10;

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<tr[\s\S]*?</tr>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(
        "<td[^>]*class\\s*=\\s*['\\\" ]r_td_3['\\\" ][^>]*>([\\s\\S]*?)</td>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex TitleCellRegex();

    [GeneratedRegex(
        "<td[^>]*class\\s*=\\s*['\\\" ]r_td_4['\\\" ][^>]*>([\\s\\S]*?)</td>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex ArtistCellRegex();

    [GeneratedRegex(
        @"http://5sing\.kugou\.com/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex SingerIdRegex();

    [GeneratedRegex(
        @"http://5sing\.kugou\.com/.+?(\d+)\.html",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex SongIdRegex();

    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;

    private bool _fcEnd;
    private bool _ycEnd;
    private bool _bzEnd;

    public FiveSingMusicApiService()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
        };
        _client = new HttpClient(_clientHandler);
    }

    public Task<JsonObject> SearchMusicAsync(string query, int page) =>
        SearchAsync(query, page, 0, 0);

    public Task<JsonObject> SearchAlbumAsync(string query, int page) =>
        SearchAsync(query, page, 1, 0);

    public Task<JsonObject> SearchArtistAsync(string query, int page) =>
        SearchAsync(query, page, 2, 1);

    public async Task<JsonObject> GetArtistWorksAsync(JsonObject artistItem, int page, string type)
    {
        ArgumentNullException.ThrowIfNull(artistItem);

        if (!string.Equals(type, "music", StringComparison.Ordinal))
        {
            return new JsonObject { ["isEnd"] = true, ["data"] = new JsonArray() };
        }

        return await GetArtistMusicWorksAsync(artistItem, page);
    }

    public async Task<JsonObject> GetLyricAsync(JsonObject musicItem)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var id =
            GetString(musicItem, "id")
            ?? throw new ArgumentException("musicItem.id 不能为空", nameof(musicItem));
        var typeEname =
            GetString(musicItem, "typeEname")
            ?? throw new ArgumentException("musicItem.typeEname 不能为空", nameof(musicItem));

        var res = await GetJsonAsync(
            "http://5sing.kugou.com/fm/m/json/lrc",
            new QueryCollection { { "songId", id }, { "songType", typeEname } },
            null
        );

        return new JsonObject { ["rawLrc"] = GetString(res, "txt") ?? string.Empty };
    }

    public async Task<JsonObject> GetAlbumInfoAsync(JsonObject albumItem)
    {
        ArgumentNullException.ThrowIfNull(albumItem);

        var id =
            GetString(albumItem, "id")
            ?? throw new ArgumentException("albumItem.id 不能为空", nameof(albumItem));

        var res = await GetJsonAsync(
            "http://service.5sing.kugou.com/song/getPlayListSong",
            new QueryCollection { { "id", id } },
            BuildMobileHeaders()
        );

        var musicList = new JsonArray();
        foreach (var item in res["data"]?.AsArray() ?? [])
        {
            if (item is not JsonObject song)
            {
                continue;
            }

            musicList.Add(
                new JsonObject
                {
                    ["id"] = GetString(song, "ID"),
                    ["typeEname"] = GetString(song, "SK"),
                    ["title"] = GetString(song, "SN"),
                    ["artist"] = GetString(song["user"] as JsonObject, "NN"),
                    ["singerId"] = GetString(song["user"] as JsonObject, "ID"),
                    ["album"] = GetString(albumItem, "title"),
                    ["artwork"] = GetString(albumItem, "artwork"),
                }
            );
        }

        return new JsonObject { ["musicList"] = musicList };
    }

    public static Task<JsonArray> GetTopListsAsync()
    {
        return Task.FromResult(
            new JsonArray
            {
                new JsonObject
                {
                    ["title"] = "排行榜",
                    ["data"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = "/",
                            ["title"] = "原创音乐榜",
                            ["description"] = "最热门的原创音乐歌曲榜",
                            ["typeEname"] = "yc",
                            ["typeName"] = "原唱",
                        },
                        new JsonObject
                        {
                            ["id"] = "/fc",
                            ["title"] = "翻唱音乐榜",
                            ["description"] = "最热门的流行歌曲翻唱排行",
                            ["typeEname"] = "fc",
                            ["typeName"] = "翻唱",
                        },
                        new JsonObject
                        {
                            ["id"] = "/bz",
                            ["title"] = "伴奏音乐榜",
                            ["description"] = "搜索最多的伴奏排行",
                            ["typeEname"] = "bz",
                            ["typeName"] = "伴奏",
                        },
                    },
                },
            }
        );
    }

    public async Task<JsonObject> GetTopListDetailAsync(JsonObject topListItem)
    {
        ArgumentNullException.ThrowIfNull(topListItem);

        var id = GetString(topListItem, "id") ?? string.Empty;
        var typeEname = GetString(topListItem, "typeEname") ?? string.Empty;
        var typeName = GetString(topListItem, "typeName") ?? string.Empty;

        var html = await GetStringAsync($"http://5sing.kugou.com/top{id}", null, null);
        var rows = TableRowRegex().Matches(html);

        var list = new JsonArray();
        foreach (var row in rows.Skip(1))
        {
            var rowText = row.Value;
            var title = HtmlToText(TitleCellRegex().Match(rowText).Groups[1].Value).Trim();
            var artistRaw = ArtistCellRegex().Match(rowText).Groups[1].Value;
            var artist = HtmlToText(artistRaw).Trim();

            var singerId = SingerIdRegex().Match(artistRaw).Groups[1].Value;
            var songId = SongIdRegex().Match(rowText).Groups[1].Value;
            if (string.IsNullOrWhiteSpace(songId))
            {
                continue;
            }

            list.Add(
                new JsonObject
                {
                    ["title"] = title,
                    ["artist"] = artist,
                    ["singerId"] = singerId,
                    ["id"] = songId,
                    ["typeEname"] = typeEname,
                    ["typeName"] = typeName,
                    ["type"] = typeEname,
                    ["album"] = typeName,
                }
            );
        }

        var result = CloneObject(topListItem);
        result["musicList"] = list;
        return result;
    }

    public async Task<JsonObject?> GetMediaSourceAsync(JsonObject musicItem, string quality)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        if (string.Equals(quality, "super", StringComparison.Ordinal))
        {
            return null;
        }

        var songId = GetString(musicItem, "id");
        var songType = GetString(musicItem, "typeEname");
        if (string.IsNullOrWhiteSpace(songId) || string.IsNullOrWhiteSpace(songType))
        {
            return null;
        }

        var query = new QueryCollection
        {
            { "songid", songId },
            { "songtype", songType },
            { "from", "web" },
            { "version", "6.6.72" },
            {
                "_",
                DateTimeOffset
                    .UtcNow.ToUnixTimeMilliseconds()
                    .ToString(CultureInfo.InvariantCulture)
            },
        };

        var raw = await GetStringAsync(
            "http://service.5sing.kugou.com/song/getsongurl",
            query,
            BuildCommonHeaders()
        );

        var trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '(' && trimmed[^1] == ')')
        {
            trimmed = trimmed[1..^1];
        }

        var root = JsonNode.Parse(trimmed)?.AsObject();
        if (root?["data"] is not JsonObject data)
        {
            return null;
        }

        var url = quality switch
        {
            "standard" => GetString(data, "squrl") ?? GetString(data, "squrl_backup"),
            "high" => GetString(data, "hqurl") ?? GetString(data, "hqurl_backup"),
            _ => GetString(data, "lqurl") ?? GetString(data, "lqurl_backup"),
        };

        return string.IsNullOrWhiteSpace(url) ? null : new JsonObject { ["url"] = url };
    }

    private async Task<JsonObject> SearchAsync(string query, int page, int type, int filter)
    {
        var res = await GetJsonAsync(
            "http://search.5sing.kugou.com/home/json",
            new QueryCollection
            {
                { "keyword", query },
                { "sort", "1" },
                { "page", page.ToString(CultureInfo.InvariantCulture) },
                { "filter", filter.ToString(CultureInfo.InvariantCulture) },
                { "type", type.ToString(CultureInfo.InvariantCulture) },
            },
            BuildSearchHeaders()
        );

        var data = new JsonArray();
        foreach (var item in res["list"]?.AsArray() ?? [])
        {
            if (item is not JsonObject source)
            {
                continue;
            }

            data.Add(
                type switch
                {
                    0 => FormatMusicItem(source),
                    1 => FormatAlbumItem(source),
                    2 => FormatArtistItem(source),
                    _ => source.DeepClone(),
                }
            );
        }

        var cur = res["pageInfo"]?["cur"]?.GetValue<int>() ?? 0;
        var totalPages = res["pageInfo"]?["totalPages"]?.GetValue<int>() ?? 0;

        return new JsonObject { ["isEnd"] = cur >= totalPages, ["data"] = data };
    }

    private async Task<JsonObject> GetArtistMusicWorksAsync(JsonObject artistItem, int page)
    {
        if (page == 1)
        {
            _fcEnd = false;
            _ycEnd = false;
            _bzEnd = false;
        }

        var artistId =
            GetString(artistItem, "id")
            ?? throw new ArgumentException("artistItem.id 不能为空", nameof(artistItem));
        var artistName = GetString(artistItem, "name") ?? string.Empty;

        var data = new JsonArray();
        if (!_fcEnd)
        {
            var fc = await GetArtistTypeSongListAsync(artistId, page, "fc");
            if ((fc["count"]?.GetValue<int>() ?? 0) <= page * PageSize)
            {
                _fcEnd = true;
            }

            AppendArtistSongs(data, fc, artistName, "fc", "翻唱");
        }

        if (!_ycEnd)
        {
            var yc = await GetArtistTypeSongListAsync(artistId, page, "yc");
            if ((yc["count"]?.GetValue<int>() ?? 0) <= page * PageSize)
            {
                _ycEnd = true;
            }

            AppendArtistSongs(data, yc, artistName, "yc", "原唱");
        }

        if (!_bzEnd)
        {
            var bz = await GetArtistTypeSongListAsync(artistId, page, "bz");
            if ((bz["count"]?.GetValue<int>() ?? 0) <= page * PageSize)
            {
                _bzEnd = true;
            }

            AppendArtistSongs(data, bz, artistName, "bz", "伴奏");
        }

        return new JsonObject { ["isEnd"] = _fcEnd && _ycEnd && _bzEnd, ["data"] = data };
    }

    private async Task<JsonObject> GetArtistTypeSongListAsync(
        string artistId,
        int page,
        string type
    )
    {
        return await GetJsonAsync(
            "http://service.5sing.kugou.com/user/songlist",
            new QueryCollection
            {
                { "userId", artistId },
                { "type", type },
                { "pageSize", PageSize.ToString(CultureInfo.InvariantCulture) },
                { "page", page.ToString(CultureInfo.InvariantCulture) },
            },
            BuildMobileHeaders()
        );
    }

    private static void AppendArtistSongs(
        JsonArray container,
        JsonObject source,
        string artistName,
        string typeEname,
        string typeName
    )
    {
        foreach (var item in source["data"]?.AsArray() ?? [])
        {
            if (item is not JsonObject song)
            {
                continue;
            }

            container.Add(
                new JsonObject
                {
                    ["id"] = GetString(song, "songId"),
                    ["artist"] = artistName,
                    ["title"] = GetString(song, "songName"),
                    ["typeEname"] = typeEname,
                    ["typeName"] = typeName,
                    ["type"] = GetString(song, "songType"),
                    ["album"] = typeName,
                }
            );
        }
    }

    private async Task<JsonObject> GetJsonAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>>? query,
        IEnumerable<KeyValuePair<string, string>>? headers
    )
    {
        var text = await GetStringAsync(url, query, headers);
        return JsonNode.Parse(text)?.AsObject() ?? [];
    }

    private async Task<string> GetStringAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>>? query,
        IEnumerable<KeyValuePair<string, string>>? headers
    )
    {
        using var response = await _client.SendAsync(
            HttpMethod.Get,
            url,
            query,
            headers,
            content: (string?)null,
            contentType: null
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static QueryCollection BuildSearchHeaders() =>
        new()
        {
            {
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36"
            },
            { "Host", "search.5sing.kugou.com" },
            { "Accept", "application/json, text/javascript, */*; q=0.01" },
            { "Accept-Encoding", "gzip, deflate" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },
            { "Referer", "http://search.5sing.kugou.com/home/index" },
        };

    private static QueryCollection BuildCommonHeaders() =>
        new()
        {
            { "Accept", "*/*" },
            { "Accept-Encoding", "gzip, deflate" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },
            { "Host", "service.5sing.kugou.com" },
            { "Referer", "http://5sing.kugou.com/" },
            {
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36"
            },
        };

    private static QueryCollection BuildMobileHeaders() =>
        new()
        {
            { "Accept", "application/json, text/plain, */*" },
            { "Accept-Encoding", "gzip, deflate" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },
            { "Cache-Control", "no-cache" },
            { "Host", "service.5sing.kugou.com" },
            { "Origin", "http://5sing.kugou.com" },
            { "Referer", "http://5sing.kugou.com/" },
            {
                "User-Agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1"
            },
        };

    private static JsonObject FormatMusicItem(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "songId"),
            ["title"] = HtmlToText(GetString(source, "songName")),
            ["artist"] = GetString(source, "singer"),
            ["singerId"] = GetString(source, "singerId"),
            ["album"] = GetString(source, "typeName"),
            ["type"] = GetString(source, "type"),
            ["typeName"] = GetString(source, "typeName"),
            ["typeEname"] = GetString(source, "typeEname"),
        };

    private static JsonObject FormatAlbumItem(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "songListId"),
            ["artist"] = GetString(source, "userName"),
            ["title"] = HtmlToText(GetString(source, "title")),
            ["artwork"] = GetString(source, "pictureUrl"),
            ["description"] = GetString(source, "content"),
            ["date"] = GetString(source, "createTime"),
        };

    private static JsonObject FormatArtistItem(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "id"),
            ["name"] = HtmlToText(GetString(source, "nickName")),
            ["fans"] = GetString(source, "fans"),
            ["avatar"] = GetString(source, "pictureUrl"),
            ["description"] = GetString(source, "description"),
            ["worksNum"] = GetString(source, "totalSong"),
        };

    private static string HtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return WebUtility.HtmlDecode(HtmlTagRegex().Replace(html, string.Empty));
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

    private static JsonObject CloneObject(JsonObject source)
    {
        var clone = new JsonObject();
        foreach (var (key, value) in source)
        {
            clone[key] = value?.DeepClone();
        }

        return clone;
    }

    public void Dispose()
    {
        _client.Dispose();
        _clientHandler.Dispose();
    }
}
