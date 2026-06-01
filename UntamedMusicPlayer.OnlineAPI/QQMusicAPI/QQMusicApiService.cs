using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.OnlineAPI.QQMusicAPI.Extensions;

namespace UntamedMusicPlayer.OnlineAPI.QQMusicAPI;

/// <summary>
/// QQ音乐 API
/// </summary>
public sealed partial class QQMusicApiService : IDisposable
{
    private const int PageSize = 20;

    private static readonly Dictionary<int, string> SearchTypeMap = new()
    {
        [0] = "song",
        [2] = "album",
        [1] = "singer",
        [3] = "songlist",
        [7] = "song",
        [12] = "mv",
    };

    private static readonly Dictionary<string, (string Prefix, string Extension)> MediaTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["m4a"] = ("C400", ".m4a"),
            ["128"] = ("M500", ".mp3"),
            ["320"] = ("M800", ".mp3"),
            ["ape"] = ("A000", ".ape"),
            ["flac"] = ("F000", ".flac"),
        };

    [GeneratedRegex("callback\\(|MusicJsonCallback\\(|jsonCallback\\(|\\)$", RegexOptions.Compiled)]
    private static partial Regex JsonpWrapperRegex();

    [GeneratedRegex(
        @"https?:\/\/i\.y\.qq\.com\/n2\/m\/share\/details\/taoge\.html\?.*id=([0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        "zh-CN"
    )]
    private static partial Regex MusicSheetShareUrlRegex();

    [GeneratedRegex(
        @"https?:\/\/y\.qq\.com\/n\/ryqq\/playlist\/([0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        "zh-CN"
    )]
    private static partial Regex MusicSheetWebUrlRegex();

    [GeneratedRegex(@"^(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex NumericIdRegex();

    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;

    public QQMusicApiService()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
        };
        _client = new HttpClient(_clientHandler);
    }

    public Task<JsonObject> SearchMusicAsync(string query, int page) => SearchAsync(query, page, 0);

    public Task<JsonObject> SearchAlbumAsync(string query, int page) => SearchAsync(query, page, 2);

    public Task<JsonObject> SearchArtistAsync(string query, int page) =>
        SearchAsync(query, page, 1);

    public Task<JsonObject> SearchMusicSheetAsync(string query, int page) =>
        SearchAsync(query, page, 3);

    public Task<JsonObject> SearchLyricAsync(string query, int page) => SearchAsync(query, page, 7);

    public async Task<JsonObject?> GetMediaSourceAsync(JsonObject musicItem, string? quality = null)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var songMid = GetString(musicItem, "songmid");
        if (string.IsNullOrEmpty(songMid))
        {
            return null;
        }

        var mediaType = quality switch
        {
            "low" => "m4a",
            "high" => "320",
            "super" => "flac",
            _ => "128",
        };

        // 与 qq.ts 保持一致：先走不带 filename 的 CgiGetVkey，请求可播放链接。
        var vKey = await GetPlayableSourceUrlAsync(songMid);
        var sip = vKey?["req_0"]?["data"]?["sip"]?.AsArray().FirstOrDefault()?.ToString();
        var purl = vKey
            ?["req_0"]?["data"]?["midurlinfo"]?.AsArray()
            .FirstOrDefault()
            ?["purl"]?.ToString();

        // 若仍无 purl，再回退到按质量拼接 filename 的旧逻辑。
        if (string.IsNullOrEmpty(purl))
        {
            var qualityVKey = await GetSourceUrlAsync(songMid, mediaType);
            sip = qualityVKey?["req_0"]?["data"]?["sip"]?.AsArray().FirstOrDefault()?.ToString();
            purl = qualityVKey
                ?["req_0"]?["data"]?["midurlinfo"]?.AsArray()
                .FirstOrDefault()
                ?["purl"]?.ToString();
        }

        if (string.IsNullOrEmpty(sip) || string.IsNullOrEmpty(purl))
        {
            return null;
        }

        return new JsonObject { ["url"] = $"{sip}{purl}" };
    }

    public async Task<JsonObject> GetAlbumInfoAsync(JsonObject albumItem)
    {
        ArgumentNullException.ThrowIfNull(albumItem);

        var albumMid = GetString(albumItem, "albumMID");
        if (string.IsNullOrEmpty(albumMid))
        {
            throw new ArgumentException("albumMID 不能为空", nameof(albumItem));
        }

        var payload = new JsonObject
        {
            ["comm"] = new JsonObject { ["ct"] = 24, ["cv"] = 10000 },
            ["albumSonglist"] = new JsonObject
            {
                ["method"] = "GetAlbumSongList",
                ["param"] = new JsonObject
                {
                    ["albumMid"] = albumMid,
                    ["albumID"] = 0,
                    ["begin"] = 0,
                    ["num"] = 999,
                    ["order"] = 2,
                },
                ["module"] = "music.musichallAlbum.AlbumSongList",
            },
        };

        var url = ChangeUrlQuery(
            new Dictionary<string, string> { ["data"] = payload.ToJsonString() },
            "https://u.y.qq.com/cgi-bin/musicu.fcg?g_tk=5381&format=json&inCharset=utf8&outCharset=utf-8"
        );
        var res = await GetJsonAsync(url);

        var musicList = new JsonArray();
        foreach (var item in res["albumSonglist"]?["data"]?["songList"]?.AsArray() ?? [])
        {
            if (item is JsonObject entry && entry["songInfo"] is JsonObject song)
            {
                musicList.Add(FormatMusicItem(song));
            }
        }

        return new JsonObject { ["musicList"] = musicList };
    }

    public Task<JsonObject> GetArtistWorksAsync(JsonObject artistItem, int page, string type) =>
        type switch
        {
            "music" => GetArtistSongsAsync(artistItem, page),
            "album" => GetArtistAlbumsAsync(artistItem, page),
            _ => Task.FromResult(new JsonObject { ["isEnd"] = true, ["data"] = new JsonArray() }),
        };

    public async Task<JsonObject> GetLyricAsync(JsonObject musicItem)
    {
        ArgumentNullException.ThrowIfNull(musicItem);

        var songMid = GetString(musicItem, "songmid");
        if (string.IsNullOrEmpty(songMid))
        {
            throw new ArgumentException("songmid 不能为空", nameof(musicItem));
        }

        var url =
            $"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={Uri.EscapeDataString(songMid)}&pcachetime={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&g_tk=5381&loginUin=0&hostUin=0&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";
        var res = await GetJsonpAsync(url);

        var lyric = DecodeLyricBase64(GetString(res, "lyric"));
        var trans = DecodeLyricBase64(GetString(res, "trans"));

        var result = new JsonObject { ["rawLrc"] = lyric ?? string.Empty };
        if (!string.IsNullOrEmpty(trans))
        {
            result["translation"] = trans;
        }

        return result;
    }

    public async Task<JsonArray?> ImportMusicSheetAsync(string urlLike)
    {
        var id = ExtractMusicSheetId(urlLike);
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var url =
            $"https://i.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&utf8=1&disstid={Uri.EscapeDataString(id)}&loginUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";
        var res = await GetJsonpAsync(url, referer: "https://y.qq.com/n/yqq/playlist");

        var list = new JsonArray();
        foreach (
            var item in res["cdlist"]?.AsArray().FirstOrDefault()?["songlist"]?.AsArray() ?? []
        )
        {
            if (item is JsonObject song)
            {
                list.Add(FormatMusicItem(song));
            }
        }

        return list;
    }

    public async Task<JsonArray> GetTopListsAsync()
    {
        const string url =
            "https://u.y.qq.com/cgi-bin/musicu.fcg?_=1577086820633&data=%7B%22comm%22%3A%7B%22g_tk%22%3A5381%2C%22uin%22%3A123456%2C%22format%22%3A%22json%22%2C%22inCharset%22%3A%22utf-8%22%2C%22outCharset%22%3A%22utf-8%22%2C%22notice%22%3A0%2C%22platform%22%3A%22h5%22%2C%22needNewCode%22%3A1%2C%22ct%22%3A23%2C%22cv%22%3A0%7D%2C%22topList%22%3A%7B%22module%22%3A%22musicToplist.ToplistInfoServer%22%2C%22method%22%3A%22GetAll%22%2C%22param%22%3A%7B%7D%7D%7D";
        var res = await GetJsonAsync(url);

        var groups = new JsonArray();
        foreach (var item in res["topList"]?["data"]?["group"]?.AsArray() ?? [])
        {
            if (item is not JsonObject group)
            {
                continue;
            }

            var topListItems = new JsonArray();
            foreach (var top in group["toplist"]?.AsArray() ?? [])
            {
                if (top is not JsonObject topObject)
                {
                    continue;
                }

                topListItems.Add(
                    new JsonObject
                    {
                        ["id"] = GetString(topObject, "topId"),
                        ["description"] = GetString(topObject, "intro"),
                        ["title"] = GetString(topObject, "title"),
                        ["period"] = GetString(topObject, "period"),
                        ["coverImg"] =
                            GetString(topObject, "headPicUrl")
                            ?? GetString(topObject, "frontPicUrl"),
                    }
                );
            }

            groups.Add(
                new JsonObject
                {
                    ["title"] = GetString(group, "groupName"),
                    ["data"] = topListItems,
                }
            );
        }

        return groups;
    }

    public async Task<JsonObject> GetTopListDetailAsync(JsonObject topListItem)
    {
        ArgumentNullException.ThrowIfNull(topListItem);

        var topId = GetString(topListItem, "id");
        if (string.IsNullOrEmpty(topId))
        {
            throw new ArgumentException("榜单 id 不能为空", nameof(topListItem));
        }

        var period = GetString(topListItem, "period") ?? string.Empty;
        var dataObject = new JsonObject
        {
            ["detail"] = new JsonObject
            {
                ["module"] = "musicToplist.ToplistInfoServer",
                ["method"] = "GetDetail",
                ["param"] = new JsonObject
                {
                    ["topId"] = topId,
                    ["offset"] = 0,
                    ["num"] = 100,
                    ["period"] = period,
                },
            },
            ["comm"] = new JsonObject { ["ct"] = 24, ["cv"] = 0 },
        };

        var url =
            $"https://u.y.qq.com/cgi-bin/musicu.fcg?g_tk=5381&data={Uri.EscapeDataString(dataObject.ToJsonString())}";
        var res = await GetJsonAsync(url);

        var musicList = new JsonArray();
        foreach (var item in res["detail"]?["data"]?["songInfoList"]?.AsArray() ?? [])
        {
            if (item is JsonObject song)
            {
                musicList.Add(FormatMusicItem(song));
            }
        }

        var result = new JsonObject(topListItem) { ["musicList"] = musicList };
        return result;
    }

    public async Task<JsonObject> GetRecommendSheetTagsAsync()
    {
        var res = await GetJsonAsync(
            "https://c.y.qq.com/splcloud/fcgi-bin/fcg_get_diss_tag_conf.fcg?format=json&inCharset=utf8&outCharset=utf-8"
        );

        var categories = res["data"]?["categories"]?.AsArray() ?? [];
        var data = new JsonArray();
        var pinned = new JsonArray();

        foreach (var category in categories.Skip(1))
        {
            if (category is not JsonObject categoryObject)
            {
                continue;
            }

            var tags = new JsonArray();
            foreach (var item in categoryObject["items"]?.AsArray() ?? [])
            {
                if (item is not JsonObject tag)
                {
                    continue;
                }

                tags.Add(
                    new JsonObject
                    {
                        ["id"] = GetString(tag, "categoryId"),
                        ["title"] = GetString(tag, "categoryName"),
                    }
                );
            }

            if (tags.Count > 0)
            {
                pinned.Add(tags[0]!.DeepClone());
            }

            data.Add(
                new JsonObject
                {
                    ["title"] = GetString(categoryObject, "categoryGroupName"),
                    ["data"] = tags,
                }
            );
        }

        return new JsonObject { ["pinned"] = pinned, ["data"] = data };
    }

    public async Task<JsonObject> GetRecommendSheetsByTagAsync(string? tagId, int page)
    {
        var pageSize = 20;
        var query = new QueryCollection
        {
            { "inCharset", "utf8" },
            { "outCharset", "utf-8" },
            { "sortId", "5" },
            { "categoryId", string.IsNullOrEmpty(tagId) ? "10000000" : tagId },
            { "sin", (pageSize * (page - 1)).ToString(CultureInfo.InvariantCulture) },
            { "ein", (page * pageSize - 1).ToString(CultureInfo.InvariantCulture) },
        };

        var raw = await GetStringResponseAsync(
            HttpMethod.Get,
            "https://c.y.qq.com/splcloud/fcgi-bin/fcg_get_diss_by_tag.fcg",
            query,
            referer: "https://y.qq.com"
        );
        var res = ParseJsonp(raw)?["data"] as JsonObject ?? [];

        var sum = res["sum"]?.GetValue<int>() ?? 0;
        var isEnd = sum <= page * pageSize;
        var data = new JsonArray();
        foreach (var item in res["list"]?.AsArray() ?? [])
        {
            if (item is not JsonObject sheet)
            {
                continue;
            }

            data.Add(
                new JsonObject
                {
                    ["id"] = GetString(sheet, "dissid"),
                    ["createTime"] = GetString(sheet, "createTime"),
                    ["title"] = GetString(sheet, "dissname"),
                    ["artwork"] = GetString(sheet, "imgurl"),
                    ["description"] = GetString(sheet, "introduction"),
                    ["playCount"] = GetString(sheet, "listennum"),
                    ["artist"] = GetString(sheet["creator"] as JsonObject, "name") ?? string.Empty,
                }
            );
        }

        return new JsonObject { ["isEnd"] = isEnd, ["data"] = data };
    }

    public async Task<JsonObject> GetMusicSheetInfoAsync(JsonObject sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var id = GetString(sheet, "id") ?? string.Empty;
        var data = await ImportMusicSheetAsync(id) ?? [];

        return new JsonObject { ["isEnd"] = true, ["musicList"] = data };
    }

    private async Task<JsonObject> SearchAsync(string query, int page, int searchType)
    {
        var baseResult = await SearchBaseAsync(query, page, searchType);
        var data = baseResult["data"] as JsonArray ?? [];
        var mapped = new JsonArray();

        foreach (var item in data)
        {
            if (item is not JsonObject source)
            {
                continue;
            }

            mapped.Add(
                searchType switch
                {
                    0 => FormatMusicItem(source),
                    2 => FormatAlbumItem(source),
                    1 => FormatArtistItem(source),
                    3 => FormatMusicSheetItem(source),
                    7 => FormatLyricItem(source),
                    _ => source.DeepClone(),
                }
            );
        }

        return new JsonObject
        {
            ["isEnd"] = baseResult["isEnd"]?.GetValue<bool>() ?? true,
            ["data"] = mapped,
        };
    }

    private async Task<JsonObject> SearchBaseAsync(string query, int page, int searchType)
    {
        var payload = new JsonObject
        {
            ["req_1"] = new JsonObject
            {
                ["method"] = "DoSearchForQQMusicDesktop",
                ["module"] = "music.search.SearchCgiService",
                ["param"] = new JsonObject
                {
                    ["num_per_page"] = PageSize,
                    ["page_num"] = page,
                    ["query"] = query,
                    ["search_type"] = searchType,
                },
            },
        };

        var res = await PostJsonAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", payload);

        var sum = res["req_1"]?["data"]?["meta"]?["sum"]?.GetValue<int>() ?? 0;
        var listName = SearchTypeMap.GetValueOrDefault(searchType, "song");
        var list = res["req_1"]?["data"]?["body"]?[listName]?["list"]?.AsArray() ?? [];

        return new JsonObject { ["isEnd"] = sum <= page * PageSize, ["data"] = list.DeepClone() };
    }

    private async Task<JsonObject?> GetSourceUrlAsync(string id, string type)
    {
        if (!MediaTypeMap.TryGetValue(type, out var typeObj))
        {
            typeObj = MediaTypeMap["128"];
        }

        var guid = Random.Shared.Next(1, 10_000_000).ToString(CultureInfo.InvariantCulture);
        var filename = $"{typeObj.Prefix}{id}{typeObj.Extension}";

        var payload = new JsonObject
        {
            ["req_0"] = new JsonObject
            {
                ["module"] = "vkey.GetVkeyServer",
                ["method"] = "CgiGetVkey",
                ["param"] = new JsonObject
                {
                    ["filename"] = new JsonArray(filename),
                    ["guid"] = guid,
                    ["songmid"] = new JsonArray(id),
                    ["songtype"] = new JsonArray(0),
                    ["uin"] = string.Empty,
                    ["loginflag"] = 1,
                    ["platform"] = "20",
                },
            },
            ["comm"] = new JsonObject
            {
                ["uin"] = string.Empty,
                ["format"] = "json",
                ["ct"] = 19,
                ["cv"] = 0,
                ["authst"] = string.Empty,
            },
        };

        var url = ChangeUrlQuery(
            new Dictionary<string, string>
            {
                ["-"] = "getplaysongvkey",
                ["g_tk"] = "5381",
                ["loginUin"] = string.Empty,
                ["hostUin"] = "0",
                ["format"] = "json",
                ["inCharset"] = "utf8",
                ["outCharset"] = "utf-8",
                ["notice"] = "0",
                ["platform"] = "yqq.json",
                ["needNewCode"] = "0",
                ["data"] = payload.ToJsonString(),
            },
            "https://u.y.qq.com/cgi-bin/musicu.fcg"
        );

        return await GetJsonAsync(url);
    }

    private async Task<JsonObject?> GetPlayableSourceUrlAsync(string songMid)
    {
        var payload = new JsonObject
        {
            ["req_0"] = new JsonObject
            {
                ["module"] = "vkey.GetVkeyServer",
                ["method"] = "CgiGetVkey",
                ["param"] = new JsonObject
                {
                    ["guid"] = Random
                        .Shared.Next(1, 10_000_000)
                        .ToString(CultureInfo.InvariantCulture),
                    ["songmid"] = new JsonArray(songMid),
                    ["songtype"] = new JsonArray(0),
                    ["uin"] = "0",
                    ["loginflag"] = 1,
                    ["platform"] = "20",
                },
            },
            ["comm"] = new JsonObject
            {
                ["uin"] = "0",
                ["format"] = "json",
                ["ct"] = 24,
                ["cv"] = 0,
            },
        };

        var url =
            $"https://u.y.qq.com/cgi-bin/musicu.fcg?data={Uri.EscapeDataString(payload.ToJsonString())}";
        return await GetJsonAsync(url);
    }

    private async Task<JsonObject> GetArtistSongsAsync(JsonObject artistItem, int page)
    {
        var singerMid = GetString(artistItem, "singerMID");
        if (string.IsNullOrEmpty(singerMid))
        {
            throw new ArgumentException("singerMID 不能为空", nameof(artistItem));
        }

        var payload = new JsonObject
        {
            ["comm"] = new JsonObject { ["ct"] = 24, ["cv"] = 0 },
            ["singer"] = new JsonObject
            {
                ["method"] = "get_singer_detail_info",
                ["param"] = new JsonObject
                {
                    ["sort"] = 5,
                    ["singermid"] = singerMid,
                    ["sin"] = (page - 1) * PageSize,
                    ["num"] = PageSize,
                },
                ["module"] = "music.web_singer_info_svr",
            },
        };

        var url = ChangeUrlQuery(
            new Dictionary<string, string> { ["data"] = payload.ToJsonString() },
            "https://u.y.qq.com/cgi-bin/musicu.fcg"
        );
        var res = await GetJsonAsync(url);

        var songs = new JsonArray();
        foreach (var item in res["singer"]?["data"]?["songlist"]?.AsArray() ?? [])
        {
            if (item is JsonObject song)
            {
                songs.Add(FormatMusicItem(song));
            }
        }

        var total = res["singer"]?["data"]?["total_song"]?.GetValue<int>() ?? 0;
        return new JsonObject { ["isEnd"] = total <= page * PageSize, ["data"] = songs };
    }

    private async Task<JsonObject> GetArtistAlbumsAsync(JsonObject artistItem, int page)
    {
        var singerMid = GetString(artistItem, "singerMID");
        if (string.IsNullOrEmpty(singerMid))
        {
            throw new ArgumentException("singerMID 不能为空", nameof(artistItem));
        }

        var payload = new JsonObject
        {
            ["comm"] = new JsonObject { ["ct"] = 24, ["cv"] = 0 },
            ["singerAlbum"] = new JsonObject
            {
                ["method"] = "get_singer_album",
                ["param"] = new JsonObject
                {
                    ["singermid"] = singerMid,
                    ["order"] = "time",
                    ["begin"] = (page - 1) * PageSize,
                    ["num"] = PageSize,
                    ["exstatus"] = 1,
                },
                ["module"] = "music.web_singer_info_svr",
            },
        };

        var url = ChangeUrlQuery(
            new Dictionary<string, string> { ["data"] = payload.ToJsonString() },
            "https://u.y.qq.com/cgi-bin/musicu.fcg"
        );
        var res = await GetJsonAsync(url);

        var albums = new JsonArray();
        foreach (var item in res["singerAlbum"]?["data"]?["list"]?.AsArray() ?? [])
        {
            if (item is JsonObject album)
            {
                albums.Add(FormatAlbumItem(album));
            }
        }

        var total = res["singerAlbum"]?["data"]?["total"]?.GetValue<int>() ?? 0;
        return new JsonObject { ["isEnd"] = total <= page * PageSize, ["data"] = albums };
    }

    private async Task<JsonObject> GetJsonAsync(string url, string referer = "https://y.qq.com")
    {
        var response = await GetStringResponseAsync(HttpMethod.Get, url, null, referer: referer);
        return JsonNode.Parse(response)?.AsObject() ?? [];
    }

    private async Task<JsonObject> PostJsonAsync(
        string url,
        JsonObject payload,
        string referer = "https://y.qq.com"
    )
    {
        var response = await GetStringResponseAsync(
            HttpMethod.Post,
            url,
            null,
            payload.ToJsonString(),
            "application/json",
            referer
        );
        return JsonNode.Parse(response)?.AsObject() ?? [];
    }

    private async Task<JsonObject> GetJsonpAsync(string url, string referer = "https://y.qq.com")
    {
        var response = await GetStringResponseAsync(HttpMethod.Get, url, null, referer: referer);
        return ParseJsonp(response) ?? [];
    }

    private async Task<string> GetStringResponseAsync(
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>>? query,
        string? content = null,
        string? contentType = null,
        string? referer = null
    )
    {
        var headers = new QueryCollection { { "User-Agent", ChooseUserAgent() } };
        if (!string.IsNullOrEmpty(referer))
        {
            headers.Add("Referer", referer);
        }

        using var response = await _client.SendAsync(
            method,
            url,
            query,
            headers,
            content,
            contentType
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static JsonObject? ParseJsonp(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var json = JsonpWrapperRegex().Replace(raw, string.Empty);
        return JsonNode.Parse(json)?.AsObject();
    }

    private static JsonObject FormatMusicItem(JsonObject source)
    {
        var albumObject = source["album"] as JsonObject;
        var albumId = GetString(source, "albumid") ?? GetString(albumObject, "id");
        var albumMid = GetString(source, "albummid") ?? GetString(albumObject, "mid");
        var albumName = GetString(source, "albumname") ?? GetString(albumObject, "title");

        var singers = source["singer"]?.AsArray() ?? [];
        var singerNames = string.Join(
            ", ",
            singers
                .OfType<JsonObject>()
                .Select(s => GetString(s, "name"))
                .Where(name => !string.IsNullOrEmpty(name))!
        );

        return new JsonObject
        {
            ["id"] = GetString(source, "id") ?? GetString(source, "songid"),
            ["songmid"] = GetString(source, "mid") ?? GetString(source, "songmid"),
            ["title"] = GetString(source, "title") ?? GetString(source, "songname"),
            ["artist"] = singerNames,
            ["artwork"] = string.IsNullOrEmpty(albumMid)
                ? null
                : $"https://y.gtimg.cn/music/photo_new/T002R800x800M000{albumMid}.jpg",
            ["album"] = albumName,
            ["lrc"] = GetString(source, "lyric"),
            ["albumid"] = albumId,
            ["albummid"] = albumMid,
        };
    }

    private static JsonObject FormatAlbumItem(JsonObject source)
    {
        var albumMid = GetString(source, "albumMID") ?? GetString(source, "album_mid");

        return new JsonObject
        {
            ["id"] = GetString(source, "albumID") ?? GetString(source, "albumid"),
            ["albumMID"] = albumMid,
            ["title"] = GetString(source, "albumName") ?? GetString(source, "album_name"),
            ["artwork"] =
                GetString(source, "albumPic")
                ?? (
                    string.IsNullOrEmpty(albumMid)
                        ? null
                        : $"https://y.gtimg.cn/music/photo_new/T002R300x300M000{albumMid}.jpg"
                ),
            ["date"] = GetString(source, "publicTime") ?? GetString(source, "pub_time"),
            ["singerID"] = GetString(source, "singerID") ?? GetString(source, "singer_id"),
            ["artist"] = GetString(source, "singerName") ?? GetString(source, "singer_name"),
            ["singerMID"] = GetString(source, "singerMID") ?? GetString(source, "singer_mid"),
            ["description"] = GetString(source, "desc"),
        };
    }

    private static JsonObject FormatArtistItem(JsonObject source) =>
        new()
        {
            ["name"] = GetString(source, "singerName"),
            ["id"] = GetString(source, "singerID"),
            ["singerMID"] = GetString(source, "singerMID"),
            ["avatar"] = GetString(source, "singerPic"),
            ["worksNum"] = GetString(source, "songNum"),
        };

    private static JsonObject FormatMusicSheetItem(JsonObject source) =>
        new()
        {
            ["title"] = GetString(source, "dissname"),
            ["createAt"] = GetString(source, "createtime"),
            ["description"] = GetString(source, "introduction"),
            ["playCount"] = GetString(source, "listennum"),
            ["worksNums"] = GetString(source, "song_count"),
            ["artwork"] = GetString(source, "imgurl"),
            ["id"] = GetString(source, "dissid"),
            ["artist"] = GetString(source["creator"] as JsonObject, "name"),
        };

    private static JsonObject FormatLyricItem(JsonObject source)
    {
        var music = FormatMusicItem(source);
        music["rawLrcTxt"] = GetString(source, "content");
        return music;
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

    private static string ChangeUrlQuery(IReadOnlyDictionary<string, string> query, string baseUrl)
    {
        var index = baseUrl.IndexOf('?', StringComparison.Ordinal);
        var url = index == -1 ? baseUrl : baseUrl[..index];
        var currentQuery = new Dictionary<string, string>(StringComparer.Ordinal);

        if (index != -1)
        {
            var queryString = baseUrl[(index + 1)..];
            foreach (var item in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var temp = item.Split('=', 2, StringSplitOptions.None);
                var key = temp[0];
                var value = temp.Length > 1 ? Uri.UnescapeDataString(temp[1]) : string.Empty;
                currentQuery[key] = value;
            }
        }

        foreach (var (key, value) in query)
        {
            currentQuery[key] = value;
        }

        var builder = new StringBuilder();
        foreach (var (key, value) in currentQuery)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
        }

        return builder.Length > 0 ? $"{url}?{builder}" : url;
    }

    private static string? DecodeLyricBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return WebUtility.HtmlDecode(Encoding.UTF8.GetString(bytes));
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractMusicSheetId(string urlLike)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlLike);

        var shareMatch = MusicSheetShareUrlRegex().Match(urlLike);
        if (shareMatch.Success)
        {
            return shareMatch.Groups[1].Value;
        }

        var webMatch = MusicSheetWebUrlRegex().Match(urlLike);
        if (webMatch.Success)
        {
            return webMatch.Groups[1].Value;
        }

        var numericMatch = NumericIdRegex().Match(urlLike);
        return numericMatch.Success ? numericMatch.Groups[1].Value : null;
    }

    private static string ChooseUserAgent() =>
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    public void Dispose()
    {
        _client.Dispose();
        _clientHandler.Dispose();
    }
}
