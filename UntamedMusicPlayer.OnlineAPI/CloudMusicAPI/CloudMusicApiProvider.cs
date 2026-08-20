using System.Net;
using System.Security.Cryptography;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.Utils;
using static UntamedMusicPlayer.OnlineAPI.CloudMusicAPI.CloudMusicApiProvider;

namespace UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;

/// <summary>
/// 网易云音乐 API 请求配置。
/// </summary>
public sealed class CloudMusicApiProvider
{
    private static readonly IEnumerable<KeyValuePair<string, string>> _emptyData = [];

    private readonly string _route;
    private readonly ParameterInfo[] _parameterInfos;

    internal HttpMethod Method { get; }
    internal Options Options { get; }
    internal Func<Dictionary<string, string>, string> Url { get; }
    internal Func<
        Dictionary<string, string>,
        IEnumerable<KeyValuePair<string, string>>
    > Data { get; }

    internal CloudMusicApiProvider(
        string name,
        HttpMethod method,
        Func<Dictionary<string, string>, string> url,
        ParameterInfo[] parameterInfos,
        Options options
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(parameterInfos);
        ArgumentNullException.ThrowIfNull(options);

        _route = name;
        _parameterInfos = parameterInfos;
        Method = method;
        Url = url;
        Options = options;
        Data = GetData;
    }

    private IEnumerable<KeyValuePair<string, string>> GetData(Dictionary<string, string> queries)
    {
        if (_parameterInfos.Length == 0)
        {
            return _emptyData;
        }

        var data = new QueryCollection(_parameterInfos.Length);
        foreach (var parameterInfo in _parameterInfos)
        {
            switch (parameterInfo.Type)
            {
                case ParameterType.Required:
                    data.Add(
                        parameterInfo.Key,
                        parameterInfo.GetRealValue(queries[parameterInfo.GetForwardedKey()])
                    );
                    break;
                case ParameterType.Optional:
                    data.Add(
                        parameterInfo.Key,
                        queries.TryGetValue(parameterInfo.GetForwardedKey(), out var value)
                            ? parameterInfo.GetRealValue(value)
                            : parameterInfo.DefaultValue ?? ""
                    );
                    break;
                case ParameterType.Constant:
                    data.Add(parameterInfo.Key, parameterInfo.DefaultValue ?? "");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterInfo));
            }
        }

        return data;
    }

    /// <summary />
    public override string ToString() => _route;

    internal enum ParameterType
    {
        Required,
        Optional,
        Constant,
    }

    internal sealed class ParameterInfo(string key, ParameterType type, string? defaultValue)
    {
        public string Key { get; } = key;
        public ParameterType Type { get; } = type;
        public string? DefaultValue { get; } = defaultValue;
        public string? KeyForwarding { get; init; }
        public Func<string, string>? Transformer { get; init; }

        public ParameterInfo(string key)
            : this(key, ParameterType.Required, null) { }

        public string GetForwardedKey() => KeyForwarding ?? Key;

        public string GetRealValue(string value) => Transformer?.Invoke(value) ?? value;
    }
}

/// <summary>
/// CloudMusicApiService 使用的网易云音乐 API 配置。
/// </summary>
public static class CloudMusicApiProviders
{
    /// <summary>
    /// 获取专辑内容。
    /// </summary>
    public static readonly CloudMusicApiProvider Album = new(
        "/album",
        HttpMethod.Post,
        q => $"https://music.163.com/weapi/v1/album/{q["id"]}",
        [],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 歌手专辑列表。
    /// </summary>
    public static readonly CloudMusicApiProvider ArtistAlbum = new(
        "/artist/album",
        HttpMethod.Post,
        q => $"https://music.163.com/weapi/artist/albums/{q["id"]}",
        [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "total"),
        ],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 获取歌手描述。
    /// </summary>
    public static readonly CloudMusicApiProvider ArtistDesc = new(
        "/artist/desc",
        HttpMethod.Post,
        q => "https://music.163.com/weapi/artist/introduction",
        [new("id")],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 歌词。
    /// </summary>
    public static readonly CloudMusicApiProvider Lyric = new(
        "/lyric",
        HttpMethod.Post,
        q => "https://music.163.com/weapi/song/lyric?lv=-1&kv=-1&tv=-1",
        [new("id")],
        BuildOptions("linuxapi")
    );

    /// <summary>
    /// 获取歌单详情。
    /// </summary>
    public static readonly CloudMusicApiProvider PlaylistDetail = new(
        "/playlist/detail",
        HttpMethod.Post,
        q => "https://music.163.com/weapi/v3/playlist/detail",
        [
            new("id"),
            new("n", ParameterType.Constant, "100000"),
            new("s", ParameterType.Optional, "8"),
        ],
        BuildOptions("linuxapi")
    );

    /// <summary>
    /// 搜索。
    /// </summary>
    public static readonly CloudMusicApiProvider Search = new(
        "/search",
        HttpMethod.Post,
        q => "https://music.163.com/weapi/search/get",
        [
            new("s") { KeyForwarding = "keywords" },
            new("type", ParameterType.Optional, "1"),
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
        ],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 搜索建议。
    /// </summary>
    public static readonly CloudMusicApiProvider SearchSuggest = new(
        "/search/suggest",
        HttpMethod.Post,
        q =>
            $"https://music.163.com/weapi/search/suggest/{(q.TryGetValue("type", out var suggestType) && suggestType == "mobile" ? "keyword" : "web")}",
        [new("s") { KeyForwarding = "keywords" }],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 获取歌曲详情。
    /// </summary>
    public static readonly CloudMusicApiProvider SongDetail = new(
        "/song/detail",
        HttpMethod.Post,
        q => "https://music.163.com/weapi/v3/song/detail",
        [
            new("c")
            {
                KeyForwarding = "ids",
                Transformer = t =>
                    "["
                    + string.Join(",", t.Split(',').Select(m => "{\"id\":" + m.Trim() + "}"))
                    + "]",
            },
            new("ids") { Transformer = JsonArrayTransformer },
        ],
        BuildOptions("weapi")
    );

    /// <summary>
    /// 获取歌曲播放地址。
    /// </summary>
    public static readonly CloudMusicApiProvider SongUrl = new(
        "/song/url",
        HttpMethod.Post,
        q => "https://music.163.com/api/song/enhance/player/url",
        [
            new("ids") { KeyForwarding = "id", Transformer = JsonArrayTransformer },
            new("br", ParameterType.Optional, "1999000"),
        ],
        BuildOptions(
            "linuxapi",
            [
                new("os", "pc"),
                new("_ntes_nuid", RandomNumberGenerator.GetBytes(16).ToHexStringLower()),
            ]
        )
    );

    private static Options BuildOptions(string crypto, IEnumerable<Cookie>? cookies = null)
    {
        ArgumentNullException.ThrowIfNull(crypto);

        var cookieCollection = new CookieCollection();
        if (cookies is not null)
        {
            foreach (var cookie in cookies)
            {
                cookieCollection.Add(cookie);
            }
        }

        return new Options { Crypto = crypto, Cookie = cookieCollection };
    }

    private static string JsonArrayTransformer(string value) => "[" + value.Replace(" ", "") + "]";
}
