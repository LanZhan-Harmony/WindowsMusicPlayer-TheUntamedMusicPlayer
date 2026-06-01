using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.OnlineAPI.KuwoMusicAPI.Extensions;

namespace UntamedMusicPlayer.OnlineAPI.KuwoMusicAPI;

/// <summary>
/// 酷我音乐 API
/// </summary>
public sealed partial class KuwoMusicApiService : IDisposable
{
    private const int PageSize = 20;

    [GeneratedRegex(
        @"https?:\/\/www\.kuwo\.cn\/playlist_detail\/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex PlaylistWebRegex();

    [GeneratedRegex(
        @"https?:\/\/m\.kuwo\.cn\/h5app\/playlist\/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    )]
    private static partial Regex PlaylistMobileRegex();

    [GeneratedRegex(@"^\s*(\d+)\s*$", RegexOptions.Compiled)]
    private static partial Regex NumericIdRegex();

    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;

    public KuwoMusicApiService()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
        };
        _client = new HttpClient(_clientHandler);
    }

    public Task<JsonObject> SearchMusicAsync(string query, int page) =>
        SearchAsync(query, page, "music");

    public Task<JsonObject> SearchAlbumAsync(string query, int page) =>
        SearchAsync(query, page, "album");

    public Task<JsonObject> SearchArtistAsync(string query, int page) =>
        SearchAsync(query, page, "artist");

    public Task<JsonObject> SearchMusicSheetAsync(string query, int page) =>
        SearchAsync(query, page, "playlist");

    public async Task<JsonObject?> GetMediaSourceAsync(JsonObject musicItem, string? quality)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        if (!string.Equals(quality, "standard", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var id = GetString(musicItem, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var res = await GetJsonAsync(
            "https://antiserver.kuwo.cn/anti.s",
            new QueryCollection
            {
                { "type", "convert_url3" },
                { "rid", id },
                { "format", "mp3" },
            }
        );

        var url = GetString(res, "url");
        return string.IsNullOrWhiteSpace(url) ? null : new JsonObject { ["url"] = url };
    }

    public async Task<JsonObject> GetArtistWorksAsync(JsonObject artistItem, int page, string type)
    {
        ArgumentNullException.ThrowIfNull(artistItem);

        return type switch
        {
            "music" => await GetArtistMusicWorksAsync(artistItem, page),
            "album" => await GetArtistAlbumWorksAsync(artistItem, page),
            _ => new JsonObject { ["isEnd"] = true, ["data"] = new JsonArray() },
        };
    }

    public async Task<JsonObject> GetLyricAsync(JsonObject musicItem)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var id =
            GetString(musicItem, "id")
            ?? throw new ArgumentException("musicItem.id 不能为空", nameof(musicItem));

        var res = await GetJsonAsync(
            "http://m.kuwo.cn/newh5/singles/songinfoandlrc",
            new QueryCollection { { "musicId", id }, { "httpStatus", "1" } }
        );

        var lines = new StringBuilder();
        foreach (var item in res["data"]?["lrclist"]?.AsArray() ?? [])
        {
            if (item is not JsonObject lyricLine)
            {
                continue;
            }

            var time = GetString(lyricLine, "time") ?? string.Empty;
            var lyric = GetString(lyricLine, "lineLyric") ?? string.Empty;
            if (lines.Length > 0)
            {
                lines.Append('\n');
            }

            lines.Append('[').Append(time).Append(']').Append(lyric);
        }

        return new JsonObject { ["rawLrc"] = lines.ToString() };
    }

    public async Task<JsonObject> GetAlbumInfoAsync(JsonObject albumItem)
    {
        ArgumentNullException.ThrowIfNull(albumItem);

        var albumId =
            GetString(albumItem, "id")
            ?? throw new ArgumentException("albumItem.id 不能为空", nameof(albumItem));

        var res = await GetJsonAsync(
            "http://search.kuwo.cn/r.s",
            new QueryCollection
            {
                { "pn", "0" },
                { "rn", "100" },
                { "albumid", albumId },
                { "stype", "albuminfo" },
                { "sortby", "0" },
                { "alflac", "1" },
                { "show_copyright_off", "1" },
                { "pcmp4", "1" },
                { "encoding", "utf8" },
                { "plat", "pc" },
                { "thost", "search.kuwo.cn" },
                { "vipver", "MUSIC_9.1.1.2_BCS2" },
                { "devid", "38668888" },
                { "newver", "1" },
                { "pcjson", "1" },
            }
        );

        var musicList = new JsonArray();
        foreach (var song in res["musiclist"]?.AsArray() ?? [])
        {
            if (song is not JsonObject item || !MusicListFilter(item))
            {
                continue;
            }

            musicList.Add(
                new JsonObject
                {
                    ["id"] = GetString(item, "id"),
                    ["artwork"] = GetString(albumItem, "artwork") ?? GetString(res, "img"),
                    ["title"] = HtmlDecode(GetString(item, "name")),
                    ["artist"] = HtmlDecode(GetString(item, "artist")),
                    ["album"] = HtmlDecode(GetString(item, "album")),
                    ["albumId"] = albumId,
                    ["artistId"] = GetString(item, "artistid"),
                    ["formats"] = GetString(item, "formats"),
                }
            );
        }

        return new JsonObject { ["musicList"] = musicList };
    }

    public async Task<JsonArray> GetTopListsAsync()
    {
        var res = await GetJsonAsync("http://wapi.kuwo.cn/api/pc/bang/list");

        var result = new JsonArray();
        foreach (var group in res["child"]?.AsArray() ?? [])
        {
            if (group is not JsonObject g)
            {
                continue;
            }

            var data = new JsonArray();
            foreach (var item in g["child"]?.AsArray() ?? [])
            {
                if (item is not JsonObject top)
                {
                    continue;
                }

                data.Add(
                    new JsonObject
                    {
                        ["id"] = GetString(top, "sourceid"),
                        ["coverImg"] =
                            GetString(top, "pic5")
                            ?? GetString(top, "pic2")
                            ?? GetString(top, "pic"),
                        ["title"] = GetString(top, "name"),
                        ["description"] = GetString(top, "intro"),
                    }
                );
            }

            result.Add(new JsonObject { ["title"] = GetString(g, "disname"), ["data"] = data });
        }

        return result;
    }

    public async Task<JsonObject> GetTopListDetailAsync(JsonObject topListItem)
    {
        ArgumentNullException.ThrowIfNull(topListItem);

        var id =
            GetString(topListItem, "id")
            ?? throw new ArgumentException("topListItem.id 不能为空", nameof(topListItem));

        var res = await GetJsonAsync(
            "http://kbangserver.kuwo.cn/ksong.s",
            new QueryCollection
            {
                { "from", "pc" },
                { "fmt", "json" },
                { "pn", "0" },
                { "rn", "80" },
                { "type", "bang" },
                { "data", "content" },
                { "id", id },
                { "show_copyright_off", "0" },
                { "pcmp4", "1" },
                { "isbang", "1" },
                { "userid", "0" },
                { "httpStatus", "1" },
            }
        );

        var musicList = new JsonArray();
        foreach (var item in res["musiclist"]?.AsArray() ?? [])
        {
            if (item is not JsonObject song)
            {
                continue;
            }

            musicList.Add(
                new JsonObject
                {
                    ["id"] = GetString(song, "id"),
                    ["title"] = HtmlDecode(GetString(song, "name")),
                    ["artist"] = HtmlDecode(GetString(song, "artist")),
                    ["album"] = HtmlDecode(GetString(song, "album")),
                    ["albumId"] = GetString(song, "albumid"),
                    ["artistId"] = GetString(song, "artistid"),
                    ["formats"] = GetString(song, "formats"),
                }
            );
        }

        var result = new JsonObject(topListItem) { ["musicList"] = musicList };
        return result;
    }

    public async Task<JsonArray?> ImportMusicSheetAsync(string urlLike)
    {
        var id = ExtractPlaylistId(urlLike);
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var page = 1;
        var totalPage = 30;
        var musicList = new JsonArray();

        while (page < totalPage)
        {
            try
            {
                var data = await GetMusicSheetResponseByIdAsync(id, page, 80);
                var total = data["total"]?.GetValue<int>() ?? 0;
                totalPage = Math.Max(1, (int)Math.Ceiling(total / 80d));

                foreach (
                    var item in data["musicList"]?.AsArray() ?? data["musiclist"]?.AsArray() ?? []
                )
                {
                    if (item is not JsonObject song || !MusicListFilter(song))
                    {
                        continue;
                    }

                    musicList.Add(
                        new JsonObject
                        {
                            ["id"] = GetString(song, "id"),
                            ["title"] = HtmlDecode(GetString(song, "name")),
                            ["artist"] = HtmlDecode(GetString(song, "artist")),
                            ["album"] = HtmlDecode(GetString(song, "album")),
                            ["albumId"] = GetString(song, "albumid"),
                            ["artistId"] = GetString(song, "artistid"),
                            ["formats"] = GetString(song, "formats"),
                        }
                    );
                }
            }
            catch
            {
                // 与原实现一致：单页失败时忽略并继续。
            }

            await Task.Delay(Random.Shared.Next(200, 301));
            page++;
        }

        return musicList;
    }

    public async Task<JsonObject> GetRecommendSheetTagsAsync()
    {
        var res = await GetJsonAsync(
            "http://wapi.kuwo.cn/api/pc/classify/playlist/getTagList",
            new QueryCollection
            {
                { "cmd", "rcm_keyword_playlist" },
                { "user", "0" },
                { "prod", "kwplayer_pc_9.0.5.0" },
                { "vipver", "9.0.5.0" },
                { "source", "kwplayer_pc_9.0.5.0" },
                { "loginUid", "0" },
                { "loginSid", "0" },
                { "appUid", "76039576" },
            }
        );

        var data = new JsonArray();
        foreach (var group in res["data"]?.AsArray() ?? [])
        {
            if (group is not JsonObject g)
            {
                continue;
            }

            var groupData = new JsonArray();
            foreach (var item in g["data"]?.AsArray() ?? [])
            {
                if (item is not JsonObject tag)
                {
                    continue;
                }

                groupData.Add(
                    new JsonObject
                    {
                        ["id"] = GetString(tag, "id"),
                        ["digest"] = GetString(tag, "digest"),
                        ["title"] = GetString(tag, "name"),
                    }
                );
            }

            if (groupData.Count == 0)
            {
                continue;
            }

            data.Add(new JsonObject { ["title"] = GetString(g, "name"), ["data"] = groupData });
        }

        var pinned = new JsonArray
        {
            new JsonObject
            {
                ["id"] = "1848",
                ["title"] = "翻唱",
                ["digest"] = "10000",
            },
            new JsonObject
            {
                ["id"] = "621",
                ["title"] = "网络",
                ["digest"] = "10000",
            },
            new JsonObject
            {
                ["id"] = "146",
                ["title"] = "伤感",
                ["digest"] = "10000",
            },
            new JsonObject
            {
                ["id"] = "35",
                ["title"] = "欧美",
                ["digest"] = "10000",
            },
        };

        return new JsonObject { ["data"] = data, ["pinned"] = pinned };
    }

    public async Task<JsonObject> GetRecommendSheetsByTagAsync(JsonObject tag, int page)
    {
        ArgumentNullException.ThrowIfNull(tag);

        const int pageSize = 20;
        JsonObject result;

        var tagId = GetString(tag, "id");
        var digest = GetString(tag, "digest");

        if (!string.IsNullOrWhiteSpace(tagId))
        {
            if (string.Equals(digest, "10000", StringComparison.Ordinal))
            {
                var payload = await GetJsonAsync(
                    "http://wapi.kuwo.cn/api/pc/classify/playlist/getTagPlayList",
                    new QueryCollection
                    {
                        { "loginUid", "0" },
                        { "loginSid", "0" },
                        { "appUid", "76039576" },
                        { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                        { "id", tagId },
                        { "rn", pageSize.ToString(CultureInfo.InvariantCulture) },
                    }
                );
                result = payload["data"]?.AsObject() ?? [];
            }
            else
            {
                var payload = await GetJsonAsync(
                    "http://mobileinterfaces.kuwo.cn/er.s",
                    new QueryCollection
                    {
                        { "type", "get_pc_qz_data" },
                        { "f", "web" },
                        { "id", tagId },
                        { "prod", "pc" },
                    }
                );

                var data = new JsonArray();
                foreach (var group in payload["array"]?.AsArray() ?? [])
                {
                    if (group is not JsonObject g)
                    {
                        continue;
                    }

                    foreach (var item in g["list"]?.AsArray() ?? [])
                    {
                        data.Add(item?.DeepClone());
                    }
                }

                result = new JsonObject { ["total"] = 0, ["data"] = data };
            }
        }
        else
        {
            var payload = await GetJsonAsync(
                "https://wapi.kuwo.cn/api/pc/classify/playlist/getRcmPlayList",
                new QueryCollection
                {
                    { "loginUid", "0" },
                    { "loginSid", "0" },
                    { "appUid", "76039576" },
                    { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                    { "rn", pageSize.ToString(CultureInfo.InvariantCulture) },
                    { "order", "hot" },
                }
            );
            result = payload["data"]?.AsObject() ?? [];
        }

        var total = result["total"]?.GetValue<int>() ?? 0;
        var isEnd = page * pageSize >= total;
        var mapped = new JsonArray();
        foreach (var item in result["data"]?.AsArray() ?? [])
        {
            if (item is not JsonObject sheet)
            {
                continue;
            }

            mapped.Add(
                new JsonObject
                {
                    ["title"] = GetString(sheet, "name"),
                    ["artist"] = GetString(sheet, "uname"),
                    ["id"] = GetString(sheet, "id"),
                    ["artwork"] = GetString(sheet, "img"),
                    ["playCount"] = GetString(sheet, "listencnt"),
                    ["createUserId"] = GetString(sheet, "uid"),
                }
            );
        }

        return new JsonObject { ["isEnd"] = isEnd, ["data"] = mapped };
    }

    public async Task<JsonObject> GetMusicSheetInfoAsync(JsonObject sheet, int page)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var id =
            GetString(sheet, "id")
            ?? throw new ArgumentException("sheet.id 不能为空", nameof(sheet));
        var res = await GetMusicSheetResponseByIdAsync(id, page, PageSize);

        var total = res["total"]?.GetValue<int>() ?? 0;
        var list = res["musiclist"]?.AsArray() ?? [];
        var musicList = new JsonArray();

        foreach (var item in list)
        {
            if (item is not JsonObject song || !MusicListFilter(song))
            {
                continue;
            }

            musicList.Add(
                new JsonObject
                {
                    ["id"] = GetString(song, "id"),
                    ["title"] = HtmlDecode(GetString(song, "name")),
                    ["artist"] = HtmlDecode(GetString(song, "artist")),
                    ["album"] = HtmlDecode(GetString(song, "album")),
                    ["albumId"] = GetString(song, "albumid"),
                    ["artistId"] = GetString(song, "artistid"),
                    ["formats"] = GetString(song, "formats"),
                }
            );
        }

        return new JsonObject { ["isEnd"] = page * PageSize >= total, ["musicList"] = musicList };
    }

    private async Task<JsonObject> SearchAsync(string query, int page, string searchType)
    {
        var res = await GetJsonAsync(
            "http://search.kuwo.cn/r.s",
            new QueryCollection
            {
                { "all", query },
                { "ft", searchType },
                { "itemset", "web_2013" },
                { "client", "kt" },
                { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                { "rn", PageSize.ToString(CultureInfo.InvariantCulture) },
                { "rformat", "json" },
                { "encoding", "utf8" },
                { "pcjson", "1" },
            }
        );

        var mapped = new JsonArray();
        IEnumerable<JsonNode?> list = searchType switch
        {
            "music" => res["abslist"]?.AsArray() ?? [],
            "album" => res["albumlist"]?.AsArray() ?? [],
            "artist" => res["abslist"]?.AsArray() ?? [],
            "playlist" => res["abslist"]?.AsArray() ?? [],
            _ => [],
        };

        foreach (var item in list)
        {
            if (item is not JsonObject source)
            {
                continue;
            }

            if (searchType is "music" && !MusicListFilter(source))
            {
                continue;
            }

            mapped.Add(
                searchType switch
                {
                    "music" => FormatMusicItem(source),
                    "album" => FormatAlbumItem(source),
                    "artist" => FormatArtistItem(source),
                    "playlist" => FormatMusicSheet(source),
                    _ => source.DeepClone(),
                }
            );
        }

        var pn = ParseInt(GetString(res, "PN"));
        var rn = ParseInt(GetString(res, "RN"));
        var total = ParseInt(GetString(res, "TOTAL"));

        return new JsonObject { ["isEnd"] = (pn + 1) * rn >= total, ["data"] = mapped };
    }

    private async Task<JsonObject> GetArtistMusicWorksAsync(JsonObject artistItem, int page)
    {
        var id =
            GetString(artistItem, "id")
            ?? throw new ArgumentException("artistItem.id 不能为空", nameof(artistItem));

        var res = await GetJsonAsync(
            "http://search.kuwo.cn/r.s",
            new QueryCollection
            {
                { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                { "rn", PageSize.ToString(CultureInfo.InvariantCulture) },
                { "artistid", id },
                { "stype", "artist2music" },
                { "sortby", "0" },
                { "alflac", "1" },
                { "show_copyright_off", "1" },
                { "pcmp4", "1" },
                { "encoding", "utf8" },
                { "plat", "pc" },
                { "thost", "search.kuwo.cn" },
                { "vipver", "MUSIC_9.1.1.2_BCS2" },
                { "devid", "38668888" },
                { "newver", "1" },
                { "pcjson", "1" },
            }
        );

        var songs = new JsonArray();
        foreach (var item in res["musiclist"]?.AsArray() ?? [])
        {
            if (item is not JsonObject song || !MusicListFilter(song))
            {
                continue;
            }

            songs.Add(
                new JsonObject
                {
                    ["id"] = GetString(song, "musicrid"),
                    ["artwork"] = ArtworkShortToLong(GetString(song, "web_albumpic_short")),
                    ["title"] = HtmlDecode(GetString(song, "name")),
                    ["artist"] = HtmlDecode(GetString(song, "artist")),
                    ["album"] = HtmlDecode(GetString(song, "album")),
                    ["albumId"] = GetString(song, "albumid"),
                    ["artistId"] = GetString(song, "artistid"),
                    ["formats"] = GetString(song, "formats"),
                }
            );
        }

        var pn = ParseInt(GetString(res, "pn"));
        var total = ParseInt(GetString(res, "total"));
        return new JsonObject { ["isEnd"] = (pn + 1) * PageSize >= total, ["data"] = songs };
    }

    private async Task<JsonObject> GetArtistAlbumWorksAsync(JsonObject artistItem, int page)
    {
        var id =
            GetString(artistItem, "id")
            ?? throw new ArgumentException("artistItem.id 不能为空", nameof(artistItem));

        var res = await GetJsonAsync(
            "http://search.kuwo.cn/r.s",
            new QueryCollection
            {
                { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                { "rn", PageSize.ToString(CultureInfo.InvariantCulture) },
                { "artistid", id },
                { "stype", "albumlist" },
                { "sortby", "1" },
                { "alflac", "1" },
                { "show_copyright_off", "1" },
                { "pcmp4", "1" },
                { "encoding", "utf8" },
                { "plat", "pc" },
                { "thost", "search.kuwo.cn" },
                { "vipver", "MUSIC_9.1.1.2_BCS2" },
                { "devid", "38668888" },
                { "newver", "1" },
                { "pcjson", "1" },
            }
        );

        var albums = new JsonArray();
        foreach (var item in res["albumlist"]?.AsArray() ?? [])
        {
            if (item is not JsonObject album)
            {
                continue;
            }

            albums.Add(FormatAlbumItem(album));
        }

        var pn = ParseInt(GetString(res, "pn"));
        var total = ParseInt(GetString(res, "total"));
        return new JsonObject { ["isEnd"] = (pn + 1) * PageSize >= total, ["data"] = albums };
    }

    private async Task<JsonObject> GetMusicSheetResponseByIdAsync(string id, int page, int pageSize)
    {
        return await GetJsonAsync(
            "http://nplserver.kuwo.cn/pl.svc",
            new QueryCollection
            {
                { "op", "getlistinfo" },
                { "pid", id },
                { "pn", (page - 1).ToString(CultureInfo.InvariantCulture) },
                { "rn", pageSize.ToString(CultureInfo.InvariantCulture) },
                { "encode", "utf8" },
                { "keyset", "pl2012" },
                { "vipver", "MUSIC_9.1.1.2_BCS2" },
                { "newver", "1" },
            }
        );
    }

    private async Task<JsonObject> GetJsonAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>>? query = null
    )
    {
        var text = await GetStringResponseAsync(HttpMethod.Get, url, query);

        try
        {
            var node = JsonNode.Parse(text);
            if (node is JsonObject jsonObject)
            {
                return jsonObject;
            }

            if (node is JsonArray jsonArray)
            {
                return new JsonObject { ["array"] = jsonArray };
            }
        }
        catch
        {
            // 某些接口偶发返回非 JSON；保持空对象避免调用方崩溃。
        }

        return [];
    }

    private async Task<string> GetStringResponseAsync(
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>>? query
    )
    {
        var headers = new QueryCollection
        {
            { "User-Agent", ChooseUserAgent() },
            { "Referer", "http://www.kuwo.cn" },
        };

        using var response = await _client.SendAsync(
            method,
            url,
            query,
            headers,
            content: (string?)null,
            contentType: null
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static JsonObject FormatMusicItem(JsonObject source)
    {
        var musicRid =
            GetString(source, "MUSICRID")?.Replace("MUSIC_", string.Empty, StringComparison.Ordinal)
            ?? string.Empty;

        return new JsonObject
        {
            ["id"] = musicRid,
            ["artwork"] = ArtworkShortToLong(GetString(source, "web_albumpic_short")),
            ["title"] = HtmlDecode(GetString(source, "NAME")),
            ["artist"] = HtmlDecode(GetString(source, "ARTIST")),
            ["album"] = HtmlDecode(GetString(source, "ALBUM")),
            ["albumId"] = GetString(source, "ALBUMID"),
            ["artistId"] = GetString(source, "ARTISTID"),
            ["formats"] = GetString(source, "FORMATS"),
        };
    }

    private static JsonObject FormatAlbumItem(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "albumid"),
            ["artist"] = HtmlDecode(GetString(source, "artist")),
            ["title"] = HtmlDecode(GetString(source, "name")),
            ["artwork"] = GetString(source, "img") ?? ArtworkShortToLong(GetString(source, "pic")),
            ["description"] = HtmlDecode(GetString(source, "info")),
            ["date"] = GetString(source, "pub"),
            ["artistId"] = GetString(source, "artistid"),
        };

    private static JsonObject FormatArtistItem(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "ARTISTID"),
            ["avatar"] = GetString(source, "hts_PICPATH"),
            ["name"] = HtmlDecode(GetString(source, "ARTIST")),
            ["artistId"] = GetString(source, "ARTISTID"),
            ["description"] = HtmlDecode(GetString(source, "desc")),
            ["worksNum"] = GetString(source, "SONGNUM"),
        };

    private static JsonObject FormatMusicSheet(JsonObject source) =>
        new()
        {
            ["id"] = GetString(source, "playlistid"),
            ["title"] = HtmlDecode(GetString(source, "name")),
            ["artist"] = HtmlDecode(GetString(source, "nickname")),
            ["artwork"] = GetString(source, "pic"),
            ["playCount"] = GetString(source, "playcnt"),
            ["description"] = HtmlDecode(GetString(source, "intro")),
            ["worksNum"] = GetString(source, "songnum"),
        };

    private static bool MusicListFilter(JsonObject item) =>
        !string.Equals(
            GetString(item["payInfo"] as JsonObject, "listen_fragment"),
            "1",
            StringComparison.Ordinal
        );

    private static string? ArtworkShortToLong(string? albumPicShort)
    {
        if (string.IsNullOrWhiteSpace(albumPicShort))
        {
            return null;
        }

        var firstSlash = albumPicShort.IndexOf('/', StringComparison.Ordinal);
        return firstSlash == -1
            ? null
            : $"https://img4.kuwo.cn/star/albumcover/256{albumPicShort[firstSlash..]}";
    }

    private static string HtmlDecode(string? text) => WebUtility.HtmlDecode(text ?? string.Empty);

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

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0;

    private static string? ExtractPlaylistId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var web = PlaylistWebRegex().Match(value);
        if (web.Success)
        {
            return web.Groups[1].Value;
        }

        var mobile = PlaylistMobileRegex().Match(value);
        if (mobile.Success)
        {
            return mobile.Groups[1].Value;
        }

        var numeric = NumericIdRegex().Match(value);
        return numeric.Success ? numeric.Groups[1].Value : null;
    }

    private static string ChooseUserAgent() =>
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    public void Dispose()
    {
        _client.Dispose();
        _clientHandler.Dispose();
    }
}
