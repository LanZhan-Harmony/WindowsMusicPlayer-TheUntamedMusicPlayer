#pragma warning disable

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Utils;
using static The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.NeteaseCloudMusicApiProvider;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
/// <summary>
/// 网易云音乐API相关信息提供者
/// </summary>
public sealed class NeteaseCloudMusicApiProvider
{
    private static readonly IEnumerable<KeyValuePair<string, string>> _emptyData = [];

    private readonly string _route;
    private readonly ParameterInfo[] _parameterInfos;
    private readonly HttpMethod _method;
    private readonly Options _options;
    private readonly Func<Dictionary<string, string>, string> _url;
    private Func<Dictionary<string, string>, IEnumerable<KeyValuePair<string, string>>> _dataProvider;

    /// <summary />
    public string Route => _route;

    internal HttpMethod Method => _method;

    internal Func<Dictionary<string, string>, string> Url => _url;

    internal Func<Dictionary<string, string>, IEnumerable<KeyValuePair<string, string>>> Data => _dataProvider ?? GetData;

    internal Options Options => _options;

    internal Func<Dictionary<string, string>, IEnumerable<KeyValuePair<string, string>>> DataProvider
    {
        get => _dataProvider;
        set => _dataProvider = value;
    }

    internal NeteaseCloudMusicApiProvider(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        _route = name;
    }

    internal NeteaseCloudMusicApiProvider(string name, HttpMethod method, Func<Dictionary<string, string>, string> url, ParameterInfo[] parameterInfos, Options options)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(parameterInfos);
        ArgumentNullException.ThrowIfNull(options);

        _route = name;
        _method = method;
        _url = url;
        _parameterInfos = parameterInfos;
        _options = options;
    }

    private IEnumerable<KeyValuePair<string, string>> GetData(Dictionary<string, string> queries)
    {
        QueryCollection data;

        if (_parameterInfos.Length == 0)
        {
            return _emptyData;
        }

        data = [];
        foreach (var parameterInfo in _parameterInfos)
        {
            switch (parameterInfo.Type)
            {
                case ParameterType.Required:
                    data.Add(parameterInfo.Key, parameterInfo.GetRealValue(queries[parameterInfo.GetForwardedKey()]));
                    break;
                case ParameterType.Optional:
                    data.Add(parameterInfo.Key, queries.TryGetValue(parameterInfo.GetForwardedKey(), out var value) ? parameterInfo.GetRealValue(value) : parameterInfo.DefaultValue);
                    break;
                case ParameterType.Constant:
                    data.Add(parameterInfo.Key, parameterInfo.DefaultValue);
                    break;
                case ParameterType.SpecialHandle:
                    data.Add(parameterInfo.Key, parameterInfo.SpecialHandler(queries));
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
        SpecialHandle
    }

    internal sealed class ParameterInfo(string key, ParameterType type, string defaultValue)
    {
        public string Key = key;
        public ParameterType Type = type;
        public string DefaultValue = defaultValue;
        public string KeyForwarding;
        public Func<string, string> Transformer;
        public Func<Dictionary<string, string>, string> SpecialHandler;

        public ParameterInfo(string key) : this(key, ParameterType.Required, null)
        {
        }

        public string GetForwardedKey() => KeyForwarding ?? Key;

        public string GetRealValue(string value) => Transformer is null ? value : Transformer(value);
    }
}

/// <summary>
/// 已知网易云音乐API相关信息提供者
/// </summary>
public static partial class CloudMusicApiProviders
{
    /// <summary>
    /// 初始化昵称
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ActivateInitProfile = new("/activate/init/profile", HttpMethod.Post, q => "http://music.163.com/eapi/activate/initProfile", [
            new ("nickname")
        ], BuildOptions("eapi", null, null, "/api/activate/initProfile"));

    /// <summary>
    /// 获取专辑内容
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Album = new("/album", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/album/{q["id"]}", [], BuildOptions("weapi"));

    /// <summary>
    /// 专辑动态信息
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider AlbumDetailDynamic = new("/album/detail/dynamic", HttpMethod.Post, q => "https://music.163.com/api/album/detail/dynamic", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 最新专辑
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider AlbumNewest = new("/album/newest", HttpMethod.Post, q => "https://music.163.com/api/discovery/newAlbum", [], BuildOptions("weapi"));

    /// <summary>
    /// 收藏/取消收藏专辑
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider AlbumSub = new("/album/sub", HttpMethod.Post, q => $"https://music.163.com/api/album/{(q["t"] == "1" ? "sub" : "unsub")}", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 已收藏专辑列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider AlbumSublist = new("/album/sublist", HttpMethod.Post, q => "https://music.163.com/weapi/album/sublist", [
            new("limit", ParameterType.Optional, "25"),
            new("offset", ParameterType.Optional, "0"),
            new ("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 歌手单曲
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Artists = new("/artists", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/artist/{q["id"]}", [], BuildOptions("weapi"));

    /// <summary>
    /// 歌手专辑列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistAlbum = new("/artist/album", HttpMethod.Post, q => $"https://music.163.com/weapi/artist/albums/{q["id"]}", [
            new ("limit", ParameterType.Optional, "30"),
            new ("offset", ParameterType.Optional, "0"),
            new ("total", ParameterType.Constant, "total")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取歌手描述
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistDesc = new("/artist/desc", HttpMethod.Post, q => "https://music.163.com/weapi/artist/introduction", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 歌手分类列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistList = new("/artist/list", HttpMethod.Post, q => "https://music.163.com/weapi/artist/list", [
            new ("categoryCode", ParameterType.Optional, "1001") { KeyForwarding = "cat" },
            new ("initial", ParameterType.Optional, string.Empty) { Transformer = t => ((int)t[0]).ToString() },
            new ("offset", ParameterType.Optional, "0"),
            new ("limit", ParameterType.Optional, "30"),
            new ("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取歌手 mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistMv = new("/artist/mv", HttpMethod.Post, q => "https://music.163.com/weapi/artist/mvs", [
            new ("artistId") { KeyForwarding = "id" },
            new ("limit", ParameterType.Optional, "30"),
            new ("offset", ParameterType.Optional, "0"),
            new ("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 收藏/取消收藏歌手
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistSub = new("/artist/sub", HttpMethod.Post, q => $"https://music.163.com/weapi/artist/{(q["t"] == "1" ? "sub" : "unsub")}", [
            new ("artistId") { KeyForwarding = "id" },
            new ("artistIds") { KeyForwarding = "id", Transformer = JsonArrayTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 收藏的歌手列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistSublist = new("/artist/sublist", HttpMethod.Post, q => "https://music.163.com/weapi/artist/sublist", [
            new ("limit", ParameterType.Optional, "25"),
            new ("offset", ParameterType.Optional, "0"),
            new ("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 歌手热门50首歌曲
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ArtistTopSong = new("/artist/top/song", HttpMethod.Post, q => "https://music.163.com/api/artist/top/song", [
            new ("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// banner
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Banner = new("/banner", HttpMethod.Post, q => "https://music.163.com/api/v2/banner/get", [
            new ("clientType", ParameterType.Optional, "pc") { KeyForwarding = "type", Transformer = BannerTypeTransformer }
        ], BuildOptions("linuxapi"));

    /// <summary>
    /// batch批量请求接口
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Batch = new("/batch", HttpMethod.Post, q => "http://music.163.com/eapi/batch", [], BuildOptions("eapi", null, null, "/api/batch"))
    {
        DataProvider = queries =>
        {
            QueryCollection data;

            data = new QueryCollection {
                    { "e_r", "true" }
                };
            foreach (var query in queries)
            {
                if (query.Key.StartsWith("/api/", StringComparison.Ordinal))
                {
                    data.Add(query);
                }
            }

            return data;
        }
    };

    /// <summary>
    /// 发送验证码
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CaptchaSent = new("/captcha/sent", HttpMethod.Post, q => "https://music.163.com/weapi/sms/captcha/sent", [
            new ("cellphone") { KeyForwarding = "phone" },
            new ("ctcode", ParameterType.Optional, "86")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 验证验证码
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CaptchaVerify = new("/captcha/verify", HttpMethod.Post, q => "https://music.163.com/weapi/sms/captcha/verify", [
            new ("cellphone") { KeyForwarding = "phone" },
            new ("captcha"),
            new ("ctcode", ParameterType.Optional, "86")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 检测手机号码是否已注册
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CellphoneExistenceCheck = new("/cellphone/existence/check", HttpMethod.Post, q => "http://music.163.com/eapi/cellphone/existence/check", [
            new ("cellphone") { KeyForwarding = "phone" },
            new ("countrycode", ParameterType.Optional, string.Empty)
        ], BuildOptions("eapi", null, null, "/api/cellphone/existence/check"));

    /// <summary>
    /// 音乐是否可用
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CheckMusic = new("/check/music", HttpMethod.Post, q => "https://music.163.com/weapi/song/enhance/player/url", [
            new ("ids") { KeyForwarding = "id", Transformer = JsonArrayTransformer },
            new ("br", ParameterType.Optional, "999000")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 发送/删除评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Comment = new("/comment", HttpMethod.Post, q => $"https://music.163.com/weapi/resource/comments/{(q["t"] == "1" ? "add" : q["t"] == "0" ? "delete" : "reply")}", [], BuildOptions("weapi", [new("os", "pc")]))
    {
        DataProvider = queries =>
        {
            QueryCollection data;

            data = new QueryCollection {
                    { "threadId", CommentTypeTransformer(queries["type"]) + queries["id"] }
                };
            switch (queries["t"])
            {
                case "0":
                    data.Add("commentId", queries["commentId"]);
                    break;
                case "1":
                    data.Add("content", queries["content"]);
                    break;
                case "2":
                    data.Add("commentId", queries["commentId"]);
                    data.Add("content", queries["content"]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("t");
            }
            return data;
        }
    };

    /// <summary>
    /// 专辑评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentAlbum = new("/comment/album", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/R_AL_3_{q["id"]}", [
            new ("rid") { KeyForwarding = "id" },
            new ("limit", ParameterType.Optional, "20"),
            new ("offset", ParameterType.Optional, "0"),
            new ("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 电台节目评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentDj = new("/comment/dj", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/A_DJ_1_{q["id"]}", [
            new ("rid") { KeyForwarding = "id" },
            new ("limit", ParameterType.Optional, "20"),
            new ("offset", ParameterType.Optional, "0"),
            new ("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 获取动态评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentEvent = new("/comment/event", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/{q["threadId"]}", [
            new ("limit", ParameterType.Optional, "20"),
            new ("offset", ParameterType.Optional, "0"),
            new ("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 热门评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentHot = new("/comment/hot", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/hotcomments/{CommentTypeTransformer(q["type"])}{q["id"]}", [
            new("rid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0"),
            new("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 云村热评
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentHotwallList = new("/comment/hotwall/list", HttpMethod.Post, q => "https://music.163.com/api/comment/hotwall/list/get", [], BuildOptions("weapi"));

    /// <summary>
    /// 给评论点赞
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentLike = new("/comment/like", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/comment/{(q["t"] == "1" ? "like" : "unlike")}", [
            new("commentId") { KeyForwarding = "cid" },
            new("threadId", ParameterType.SpecialHandle, null) { SpecialHandler = q => q["type"] == "6" ? q["threadId"] : CommentTypeTransformer(q["type"]) + q["id"] }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 歌曲评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentMusic = new("/comment/music", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/R_SO_4_{q["id"]}", [
            new("rid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0"),
            new("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// mv 评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentMv = new("/comment/mv", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/R_MV_5_{q["id"]}", [
            new("rid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0"),
            new("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 歌单评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentPlaylist = new("/comment/playlist", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/A_PL_0_{q["id"]}", [
            new("rid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0"),
            new("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 视频评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider CommentVideo = new("/comment/video", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/resource/comments/R_VI_62_{q["id"]}", [
            new("rid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0"),
            new("beforeTime", ParameterType.Optional, "0") { KeyForwarding = "before" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 签到
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DailySignin = new("/daily_signin", HttpMethod.Post, q => "https://music.163.com/weapi/point/dailyTask", [
            new("type", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 我的数字专辑
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DigitalAlbumPurchased = new("/digitalAlbum/purchased", HttpMethod.Post, q => "https://music.163.com/api/digitalAlbum/purchased", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台banner
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjBanner = new("/dj/banner", HttpMethod.Post, q => "http://music.163.com/weapi/djradio/banner/get", [], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 电台 - 非热门类型
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjCategoryExcludehot = new("/dj/category/excludehot", HttpMethod.Post, q => "http://music.163.com/weapi/djradio/category/excludehot", [], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 推荐类型
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjCategoryRecommend = new("/dj/category/recommend", HttpMethod.Post, q => "http://music.163.com/weapi/djradio/home/category/recommend", [], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 分类
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjCatelist = new("/dj/catelist", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/category/get", [], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjDetail = new("/dj/detail", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/get", [
            new("id") { KeyForwarding = "rid" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 热门电台
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjHot = new("/dj/hot", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/hot/v1", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 付费精选
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjPaygift = new("/dj/paygift", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/home/paygift/list?_nmclfl=1", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 节目
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjProgram = new("/dj/program", HttpMethod.Post, q => "https://music.163.com/weapi/dj/program/byradio", [
            new("radioId") { KeyForwarding = "rid" },
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("asc", ParameterType.Optional, "false")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 节目详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjProgramDetail = new("/dj/program/detail", HttpMethod.Post, q => "https://music.163.com/weapi/dj/program/detail", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 节目榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjProgramToplist = new("/dj/program/toplist", HttpMethod.Post, q => "https://music.163.com/api/program/toplist/v1", [
            new("limit", ParameterType.Optional, "100"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 24小时节目榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjProgramToplistHours = new("/dj/program/toplist/hours", HttpMethod.Post, q => "https://music.163.com/api/program/toplist/hours", [
            new("limit", ParameterType.Optional, "100")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 类别热门电台
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjRadioHot = new("/dj/radio/hot", HttpMethod.Post, q => "https://music.163.com/api/djradio/hot", [
            new("cateId"),
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 推荐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjRecommend = new("/dj/recommend", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/recommend/v1", [], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 分类推荐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjRecommendType = new("/dj/recommend/type", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/recommend", [
            new("cateId") { KeyForwarding = "type" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 订阅
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjSub = new("/dj/sub", HttpMethod.Post, q => $"https://music.163.com/weapi/djradio/{(q["t"] == "1" ? "sub" : "unsub")}", [
            new("id") { KeyForwarding = "rid" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台的订阅列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjSublist = new("/dj/sublist", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/get/subed", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 今日优选
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjTodayPerfered = new("/dj/today/perfered", HttpMethod.Post, q => "http://music.163.com/weapi/djradio/home/today/perfered", [
            new("page", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 新晋电台榜/热门电台榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjToplist = new("/dj/toplist", HttpMethod.Post, q => "https://music.163.com/api/djradio/toplist", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("type", ParameterType.Optional, "new") { Transformer = DjToplistTypeTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 24小时主播榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjToplistHours = new("/dj/toplist/hours", HttpMethod.Post, q => "https://music.163.com/api/dj/toplist/hours", [
            new("limit", ParameterType.Optional, "100")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 主播新人榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjToplistNewcomer = new("/dj/toplist/newcomer", HttpMethod.Post, q => "https://music.163.com/api/dj/toplist/newcomer", [
            new("limit", ParameterType.Optional, "100")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 付费精品
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjToplistPay = new("/dj/toplist/pay", HttpMethod.Post, q => "https://music.163.com/api/djradio/toplist/pay", [
            new("limit", ParameterType.Optional, "100")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 电台 - 最热主播榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider DjToplistPopular = new("/dj/toplist/popular", HttpMethod.Post, q => "https://music.163.com/api/dj/toplist/popular", [
            new("limit", ParameterType.Optional, "100")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取动态消息
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Event = new("/event", HttpMethod.Post, q => "https://music.163.com/weapi/v1/event/get", [
            new("pagesize", ParameterType.Optional, "20"),
            new("lasttime", ParameterType.Optional, "-1")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 删除用户动态
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider EventDel = new("/event/del", HttpMethod.Post, q => "https://music.163.com/eapi/event/delete", [
            new("id") { KeyForwarding = "evId" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 转发用户动态
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider EventForward = new("/event/forward", HttpMethod.Post, q => "https://music.163.com/weapi/event/forward", [
            new("forwards"),
            new("id") { KeyForwarding = "evId" },
            new("eventUserId") { KeyForwarding = "uid" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 垃圾桶
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider FmTrash = new("/fm_trash", HttpMethod.Post, q => $"https://music.163.com/weapi/radio/trash/add?alg=RT&songId={q["id"]}&time={q.GetValueOrDefault("time", "25")}", [
            new("songId") { KeyForwarding = "id" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 关注/取消关注用户
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Follow = new("/follow", HttpMethod.Post, q => $"https://music.163.com/weapi/user/{(q["t"] == "1" ? "follow" : "delfollow")}/{q["id"]}", [], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 获取热门话题
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider HotTopic = new("/hot/topic", HttpMethod.Post, q => "http://music.163.com/weapi/act/hot", [
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 喜欢音乐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Like = new("/like", HttpMethod.Post, q => $"https://music.163.com/weapi/radio/like?alg={q.GetValueOrDefault("alg", "itembased")}&trackId={q["id"]}&time={q.GetValueOrDefault("time", "25")}", [
            new("trackId") { KeyForwarding = "id" },
            new("like")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 喜欢音乐列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Likelist = new("/likelist", HttpMethod.Post, q => "https://music.163.com/weapi/song/like/get", [
            new("uid")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 邮箱登录
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Login = new("/login", HttpMethod.Post, q => "https://music.163.com/weapi/login", [
            new("username") { KeyForwarding = "email" },
            new("password") { Transformer = t => t.ToByteArrayUtf8().ComputeMd5().ToHexStringLower() },
            new("rememberLogin", ParameterType.Constant, "true"),
        ], BuildOptions("weapi", [new("os", "pc")], "pc"));

    /// <summary>
    /// 手机登录
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider LoginCellphone = new("/login/cellphone", HttpMethod.Post, q => "https://music.163.com/weapi/login/cellphone", [
            new("phone"),
            new("countrycode", ParameterType.Optional, string.Empty),
            new("password") { Transformer = t => t.ToByteArrayUtf8().ComputeMd5().ToHexStringLower() },
            new("rememberLogin", ParameterType.Constant, "true")
        ], BuildOptions("weapi", [new("os", "pc")], "pc"));

    /// <summary>
    /// 登录刷新
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider LoginRefresh = new("/login/refresh", HttpMethod.Post, q => "https://music.163.com/weapi/login/token/refresh", [], BuildOptions("weapi", null, "pc"));

    /// <summary>
    /// 登录状态
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider LoginStatus = new("/login/status");

    /// <summary>
    /// 退出登录
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Logout = new("/logout", HttpMethod.Post, q => "https://music.163.com/weapi/logout", [], BuildOptions("weapi", null, "pc"));

    /// <summary>
    /// 歌词
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Lyric = new("/lyric", HttpMethod.Post, q => "https://music.163.com/weapi/song/lyric?lv=-1&kv=-1&tv=-1", [
            new("id")
        ], BuildOptions("linuxapi"));

    /// <summary>
    /// 通知 - 评论
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MsgComments = new("/msg/comments", HttpMethod.Post, q => $"https://music.163.com/api/v1/user/comments/{q["uid"]}", [
            new("uid"),
            new("beforeTime", ParameterType.Optional, "-1") { KeyForwarding = "before" },
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 通知 - @我
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MsgForwards = new("/msg/forwards", HttpMethod.Post, q => "https://music.163.com/api/forwards/get", [
            new("offset", ParameterType.Optional, "0"),
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 通知 - 通知
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MsgNotices = new("/msg/notices", HttpMethod.Post, q => "https://music.163.com/api/msg/notices", [
            new("offset", ParameterType.Optional, "0"),
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 通知 - 私信
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MsgPrivate = new("/msg/private", HttpMethod.Post, q => "https://music.163.com/api/msg/private/users", [
            new("offset", ParameterType.Optional, "0"),
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 私信内容
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MsgPrivateHistory = new("/msg/private/history", HttpMethod.Post, q => "https://music.163.com/api/msg/private/history", [
            new("userId") { KeyForwarding = "uid" },
            new("limit", ParameterType.Optional, "30"),
            new("time", ParameterType.Optional, "0") { KeyForwarding = "before" },
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 全部 mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvAll = new("/mv/all", HttpMethod.Post, q => "https://interface.music.163.com/api/mv/all", [
        new("tags", ParameterType.SpecialHandle, null) { SpecialHandler = q => JsonSerializer.Serialize(new QueryCollection {
            { "地区", q.GetValueOrDefault("area", "全部") },
            { "类型", q.GetValueOrDefault("type", "全部") },
            { "排序", q.GetValueOrDefault("order", "上升最快") }
        }) },
        new("limit", ParameterType.Optional, "30"),
        new("offset", ParameterType.Optional, "0"),
        new("total", ParameterType.Constant, "true")
    ], BuildOptions("weapi"));

    /// <summary>
    /// 获取 mv 数据
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvDetail = new("/mv/detail", HttpMethod.Post, q => "https://music.163.com/weapi/mv/detail", [
            new("id") { KeyForwarding = "mvid" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 网易出品mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvExclusiveRcmd = new("/mv/exclusive/rcmd", HttpMethod.Post, q => "https://interface.music.163.com/api/mv/exclusive/rcmd", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 最新 mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvFirst = new("/mv/first", HttpMethod.Post, q => "https://interface.music.163.com/weapi/mv/first", [
            new("area", ParameterType.Optional, string.Empty),
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 收藏/取消收藏 MV
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvSub = new("/mv/sub", HttpMethod.Post, q => $"https://music.163.com/weapi/mv/{(q["t"] == "1" ? "sub" : "unsub")}", [
            new("mvId") { KeyForwarding = "mvid" },
            new("mvIds") { KeyForwarding = "mvid", Transformer = JsonArrayTransformer2 }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 收藏的 MV 列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvSublist = new("/mv/sublist", HttpMethod.Post, q => "https://music.163.com/weapi/cloudvideo/allvideo/sublist", [
            new("limit", ParameterType.Optional, "25"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// mv 地址
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider MvUrl = new("/mv/url", HttpMethod.Post, q => "https://music.163.com/weapi/song/enhance/play/mv/url", [
            new("id"),
            new("r", ParameterType.Optional, "1080") { KeyForwarding = "res" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 推荐歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Personalized = new("/personalized", HttpMethod.Post, q => "https://music.163.com/weapi/personalized/playlist", [
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "true"),
            new("n", ParameterType.Constant, "1000")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 推荐电台
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PersonalizedDjprogram = new("/personalized/djprogram", HttpMethod.Post, q => "https://music.163.com/weapi/personalized/djprogram", [], BuildOptions("weapi"));

    /// <summary>
    /// 推荐 mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PersonalizedMv = new("/personalized/mv", HttpMethod.Post, q => "https://music.163.com/weapi/personalized/mv", [], BuildOptions("weapi"));

    /// <summary>
    /// 推荐新音乐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PersonalizedNewsong = new("/personalized/newsong", HttpMethod.Post, q => "https://music.163.com/weapi/personalized/newsong", [
            new("type", ParameterType.Constant, "recommend")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 独家放送
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PersonalizedPrivatecontent = new("/personalized/privatecontent", HttpMethod.Post, q => "https://music.163.com/weapi/personalized/privatecontent", [], BuildOptions("weapi"));

    /// <summary>
    /// 私人 FM
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PersonalFm = new("/personal_fm", HttpMethod.Post, q => "https://music.163.com/weapi/v1/radio/get", [], BuildOptions("weapi"));

    /// <summary>
    /// 歌单分类
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistCatlist = new("/playlist/catlist", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/catalogue", [], BuildOptions("weapi"));

    /// <summary>
    /// 新建歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistCreate = new("/playlist/create", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/create", [
            new("name"),
            new("privacy", ParameterType.Optional, string.Empty)
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 删除歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistDelete = new("/playlist/delete", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/delete", [
            new("pid") { KeyForwarding = "id" }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 更新歌单描述
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistDescUpdate = new("/playlist/desc/update", HttpMethod.Post, q => "http://interface3.music.163.com/eapi/playlist/desc/update", [
            new("id"),
            new("desc")
        ], BuildOptions("eapi", null, null, "/api/playlist/desc/update"));

    /// <summary>
    /// 获取歌单详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistDetail = new("/playlist/detail", HttpMethod.Post, q => "https://music.163.com/weapi/v3/playlist/detail", [
            new("id"),
            new("n", ParameterType.Constant, "100000"),
            new("s", ParameterType.Optional, "8")
        ], BuildOptions("linuxapi"));

    /// <summary>
    /// 热门歌单分类
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistHot = new("/playlist/hot", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/hottags", [], BuildOptions("weapi"));

    /// <summary>
    /// 更新歌单名
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistNameUpdate = new("/playlist/name/update", HttpMethod.Post, q => "http://interface3.music.163.com/eapi/playlist/update/name", [
            new("id"),
            new("name")
        ], BuildOptions("eapi", null, null, "/api/playlist/update/name"));

    /// <summary>
    /// 收藏/取消收藏歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistSubscribe = new("/playlist/subscribe", HttpMethod.Post, q => $"https://music.163.com/weapi/playlist/{(q["t"] == "1" ? "subscribe" : "unsubscribe")}", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 歌单收藏者
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistSubscribers = new("/playlist/subscribers", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/subscribers", [
            new("id"),
            new("limit", ParameterType.Optional, "20"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 更新歌单标签
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistTagsUpdate = new("/playlist/tags/update", HttpMethod.Post, q => "http://interface3.music.163.com/eapi/playlist/tags/update", [
            new("id"),
            new("tags")
        ], BuildOptions("eapi", null, null, "/api/playlist/tags/update"));

    /// <summary>
    /// 对歌单添加或删除歌曲
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistTracks = new("/playlist/tracks", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/manipulate/tracks", [
            new("op"),
            new("pid"),
            new("trackIds") { Transformer = JsonArrayTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 更新歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaylistUpdate = new("/playlist/update", HttpMethod.Post, q => "https://music.163.com/weapi/batch", [], BuildOptions("weapi", [new("os", "pc")]))
    {
        DataProvider = queries => new QueryCollection {
                { "/api/playlist/update/name", $"{{\"id\":{queries["id"]},\"name\":\"{queries["name"]}\"}}" },
                { "/api/playlist/desc/update", $"{{\"id\":{queries["id"]},\"desc\":\"{queries["desc"]}\"}}" },
                { "/api/playlist/tags/update", $"{{\"id\":{queries["id"]},\"tags\":\"{queries["tags"]}\"}}" },
            }
    };

    /// <summary>
    /// 心动模式/智能播放
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider PlaymodeIntelligenceList = new("/playmode/intelligence/list", HttpMethod.Post, q => "http://music.163.com/weapi/playmode/intelligence/list", [
            new("songId") { KeyForwarding = "id" },
            new("playlistId") { KeyForwarding = "pid" },
            new("startMusicId", ParameterType.SpecialHandle, null) { SpecialHandler = q => q.TryGetValue("sid", out var sid) ? sid : q["id"] },
            new("count", ParameterType.Optional, "1"),
            new("type", ParameterType.Constant, "fromPlayOne")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 推荐节目
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ProgramRecommend = new("/program/recommend", HttpMethod.Post, q => "https://music.163.com/weapi/program/recommend/v1", [
            new("cateId", ParameterType.Optional, string.Empty) { KeyForwarding = "type" },
            new("limit", ParameterType.Optional, "10"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 更换绑定手机
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Rebind = new("/rebind", HttpMethod.Post, q => "https://music.163.com/api/user/replaceCellphone", [
            new("captcha"),
            new("phone"),
            new("oldcaptcha"),
            new("ctcode", ParameterType.Optional, "86")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 每日推荐歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider RecommendResource = new("/recommend/resource", HttpMethod.Post, q => "https://music.163.com/weapi/v1/discovery/recommend/resource", [], BuildOptions("weapi"));

    /// <summary>
    /// 每日推荐歌曲
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider RecommendSongs = new("/recommend/songs", HttpMethod.Post, q => "https://music.163.com/weapi/v1/discovery/recommend/songs", [], BuildOptions("weapi"));

    /// <summary>
    /// 注册(修改密码)
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider RegisterCellphone = new("/register/cellphone", HttpMethod.Post, q => "https://music.163.com/weapi/register/cellphone", [
            new("captcha"),
            new("phone"),
            new("password") { Transformer = t => t.ToByteArrayUtf8().ComputeMd5().ToHexStringLower() },
            new("nickname")
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 相关视频
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider RelatedAllvideo = new("/related/allvideo", HttpMethod.Post, q => "https://music.163.com/weapi/cloudvideo/v1/allvideo/rcmd", [
            new("id"),
            new("type") { KeyForwarding = "id", Transformer = t => MyRegex().IsMatch(t) ? "0" : "1" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 相关歌单推荐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider RelatedPlaylist = new("/related/playlist");

    /// <summary>
    /// 资源点赞( MV,电台,视频)
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ResourceLike = new("/resource/like", HttpMethod.Post, q => $"https://music.163.com/weapi/resource/{(q["t"] == "1" ? "like" : "unlike")}", [
            new("threadId", ParameterType.SpecialHandle, null) { SpecialHandler = q => q["type"] == "6" ? q["threadId"] : ResourceTypeTransformer(q["type"]) + q["id"] }
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 听歌打卡
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Scrobble = new("/scrobble", HttpMethod.Post, q => "https://music.163.com/weapi/feedback/weblog", [
            new("logs", ParameterType.SpecialHandle, null) { SpecialHandler = q => JsonSerializer.Serialize(new QueryCollection {
                { "action", "play" },
                { "json", JsonSerializer.Serialize(new QueryCollection {
                    { "id", q["id"] },
                    { "sourceId", q["sourceId"] },
                    { "time", q["time"] },
                    { "download", "0" },
                    { "end", "playend" },
                    { "type", "song" },
                    { "wifi", "0" }
                }) }
            }) }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 搜索
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Search = new("/search", HttpMethod.Post, q => "https://music.163.com/weapi/search/get", [
            new("s") { KeyForwarding = "keywords" },
            new("type", ParameterType.Optional, "1"),
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 默认搜索关键词
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SearchDefault = new("/search/default", HttpMethod.Post, q => "http://interface3.music.163.com/eapi/search/defaultkeyword/get", [], BuildOptions("eapi", null, null, "/api/search/defaultkeyword/get"));

    /// <summary>
    /// 热搜列表(简略)
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SearchHot = new("/search/hot", HttpMethod.Post, q => "https://music.163.com/weapi/search/hot", [
            new("type", ParameterType.Constant, "1111")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 热搜列表(详细)
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SearchHotDetail = new("/search/hot/detail", HttpMethod.Post, q => "https://music.163.com/weapi/hotsearchlist/get", [], BuildOptions("weapi"));

    /// <summary>
    /// 搜索多重匹配
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SearchMultimatch = new("/search/multimatch", HttpMethod.Post, q => "https://music.163.com/weapi/search/suggest/multimatch", [
            new("s") { KeyForwarding = "keywords" },
            new("type", ParameterType.Optional, "1")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 搜索建议
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SearchSuggest = new("/search/suggest", HttpMethod.Post, q => $"https://music.163.com/weapi/search/suggest/{(q.GetValueOrDefault("type", null) == "mobile" ? "keyword" : "web")}", [
            new("s") { KeyForwarding = "keywords" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 发送私信(带歌单)
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SendPlaylist = new("/send/playlist", HttpMethod.Post, q => "https://music.163.com/weapi/msg/private/send", [
            new("userIds") { KeyForwarding = "user_ids", Transformer = JsonArrayTransformer },
            new("msg"),
            new("id", ParameterType.Optional, string.Empty) { KeyForwarding = "playlist" },
            new("type", ParameterType.Constant, "playlist")
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 发送私信
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SendText = new("/send/text", HttpMethod.Post, q => "https://music.163.com/weapi/msg/private/send", [
            new("userIds") { KeyForwarding = "user_ids", Transformer = JsonArrayTransformer },
            new("msg"),
            new("id", ParameterType.Optional, string.Empty) { KeyForwarding = "playlist" },
            new("type", ParameterType.Constant, "text")
        ], BuildOptions("weapi", [new("os", "pc")]));

    /// <summary>
    /// 设置
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Setting = new("/setting", HttpMethod.Post, q => "https://music.163.com/api/user/setting", [], BuildOptions("weapi"));

    /// <summary>
    /// 分享歌曲、歌单、mv、电台、电台节目到动态
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ShareResource = new("/share/resource", HttpMethod.Post, q => "http://music.163.com/weapi/share/friends/resource", [
            new("type", ParameterType.Optional, "song"),
            new("msg", ParameterType.Optional, string.Empty),
            new("id", ParameterType.Optional, string.Empty)
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取相似歌手
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SimiArtist = new("/simi/artist", HttpMethod.Post, q => "https://music.163.com/weapi/discovery/simiArtist", [
            new("artistid") { KeyForwarding = "id" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 相似 mv
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SimiMv = new("/simi/mv", HttpMethod.Post, q => "https://music.163.com/weapi/discovery/simiMV", [
            new("mvid")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取相似歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SimiPlaylist = new("/simi/playlist", HttpMethod.Post, q => "https://music.163.com/weapi/discovery/simiPlaylist", [
            new("songid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取相似音乐
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SimiSong = new("/simi/song", HttpMethod.Post, q => "https://music.163.com/weapi/v1/discovery/simiSong", [
            new("songid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取最近 5 个听了这首歌的用户
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SimiUser = new("/simi/user", HttpMethod.Post, q => "https://music.163.com/weapi/discovery/simiUser", [
            new("songid") { KeyForwarding = "id" },
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取歌曲详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SongDetail = new("/song/detail", HttpMethod.Post, q => "https://music.163.com/weapi/v3/song/detail", [
            new("c") { KeyForwarding = "ids", Transformer = t => "[" + string.Join(",", t.Split(',').Select(m => "{\"id\":" + m.Trim() + "}")) + "]" },
            new("ids") { Transformer = JsonArrayTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取音乐 url
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider SongUrl = new("/song/url", HttpMethod.Post, q => "https://music.163.com/api/song/enhance/player/url", [
            new("ids") { KeyForwarding = "id", Transformer = JsonArrayTransformer },
            new("br", ParameterType.Optional, "999000")
        ], BuildOptions("linuxapi", [new("os", "pc"), new("_ntes_nuid", new Random().RandomBytes(16).ToHexStringLower())]));

    /// <summary>
    /// 所有榜单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Toplist = new("/toplist", HttpMethod.Post, q => "https://music.163.com/weapi/toplist", [], BuildOptions("linuxapi"));

    /// <summary>
    /// 歌手榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ToplistArtist = new("/toplist/artist", HttpMethod.Post, q => "https://music.163.com/weapi/toplist/artist", [
            new("type", ParameterType.Constant, "1"),
            new("limit", ParameterType.Constant, "100"),
            new("offset", ParameterType.Constant, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 所有榜单内容摘要
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider ToplistDetail = new("/toplist/detail", HttpMethod.Post, q => "https://music.163.com/weapi/toplist/detail", [], BuildOptions("weapi"));

    /// <summary>
    /// 新碟上架
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopAlbum = new("/top/album", HttpMethod.Post, q => "https://music.163.com/weapi/album/new", [
            new("area", ParameterType.Optional, "ALL") { KeyForwarding = "type" },
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 热门歌手
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopArtists = new("/top/artists", HttpMethod.Post, q => "https://music.163.com/weapi/artist/top", [
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 排行榜
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Top_List = new("/top/list", HttpMethod.Post, q => "https://music.163.com/weapi/v3/playlist/detail", [
            new("id") { KeyForwarding = "idx", Transformer = TopListIdTransformer },
            new("n", ParameterType.Constant, "10000")
        ], BuildOptions("linuxapi"));

    /// <summary>
    /// mv 排行
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopMv = new("/top/mv", HttpMethod.Post, q => "https://music.163.com/weapi/mv/toplist", [
            new("area", ParameterType.Optional, string.Empty),
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 歌单 ( 网友精选碟 )
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopPlaylist = new("/top/playlist", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/list", [
            new("cat", ParameterType.Optional, "全部"),
            new("order", ParameterType.Optional, "hot"),
            new("limit", ParameterType.Optional, "50"),
            new("offset", ParameterType.Optional, "0"),
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取精品歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopPlaylistHighquality = new("/top/playlist/highquality", HttpMethod.Post, q => "https://music.163.com/weapi/playlist/highquality/list", [
            new("cat", ParameterType.Optional, "全部"),
            new("limit", ParameterType.Optional, "50"),
            new("lasttime", ParameterType.Optional, "0") { KeyForwarding = "before" },
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 新歌速递
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider TopSong = new("/top/song", HttpMethod.Post, q => "https://music.163.com/weapi/v1/discovery/new/songs", [
            new("areaId") { KeyForwarding = "type" },
            new("total", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 用户电台
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserAudio = new("/user/audio", HttpMethod.Post, q => "https://music.163.com/weapi/djradio/get/byuser", [
            new("userId") { KeyForwarding = "uid" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 云盘
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserCloud = new("/user/cloud", HttpMethod.Post, q => "https://music.163.com/weapi/v1/cloud/get", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 云盘歌曲删除
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserCloudDel = new("/user/cloud/del", HttpMethod.Post, q => "http://music.163.com/weapi/cloud/del", [
            new("songIds") { KeyForwarding = "id", Transformer = JsonArrayTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 云盘数据详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserCloudDetail = new("/user/cloud/detail", HttpMethod.Post, q => "https://music.163.com/weapi/v1/cloud/get/byids", [
            new("songIds") { KeyForwarding = "id", Transformer = JsonArrayTransformer }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserDetail = new("/user/detail", HttpMethod.Post, q => $"https://music.163.com/weapi/v1/user/detail/{q["uid"]}", [], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户电台
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserDj = new("/user/dj", HttpMethod.Post, q => $"https://music.163.com/weapi/dj/program/{q["uid"]}", [
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户动态
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserEvent = new("/user/event", HttpMethod.Post, q => $"https://music.163.com/weapi/event/get/{q["uid"]}", [
            new("getcounts", ParameterType.Constant, "true"),
            new("time", ParameterType.Optional, "-1") { KeyForwarding = "lasttime" },
            new("limit", ParameterType.Optional, "30"),
            new("total", ParameterType.Constant, "false")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户粉丝列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserFolloweds = new("/user/followeds", HttpMethod.Post, q => $"https://music.163.com/eapi/user/getfolloweds/{q["uid"]}", [
            new("userId") { KeyForwarding = "uid" },
            new("time", ParameterType.Optional, "-1") { KeyForwarding = "lasttime" },
            new("limit", ParameterType.Optional, "30")
        ], BuildOptions("eapi", null, null, "/api/user/getfolloweds"));

    /// <summary>
    /// 获取用户关注列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserFollows = new("/user/follows", HttpMethod.Post, q => $"https://music.163.com/weapi/user/getfollows/{q["uid"]}", [
            new("offset", ParameterType.Optional, "0"),
            new("limit", ParameterType.Optional, "30"),
            new("order", ParameterType.Constant, "true")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户歌单
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserPlaylist = new("/user/playlist", HttpMethod.Post, q => "https://music.163.com/weapi/user/playlist", [
            new("uid"),
            new("limit", ParameterType.Optional, "30"),
            new("offset", ParameterType.Optional, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户播放记录
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserRecord = new("/user/record", HttpMethod.Post, q => "https://music.163.com/weapi/v1/play/record", [
            new("uid"),
            new("type", ParameterType.Optional, "1")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取用户信息 , 歌单，收藏，mv, dj 数量
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserSubcount = new("/user/subcount", HttpMethod.Post, q => "https://music.163.com/weapi/subcount", [], BuildOptions("weapi"));

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider UserUpdate = new("/user/update", HttpMethod.Post, q => "https://music.163.com/weapi/user/profile/update", [
            new("birthday"),
            new("city"),
            new("gender"),
            new("nickname"),
            new("province"),
            new("signature"),
            new("avatarImgId", ParameterType.Constant, "0")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 视频详情
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider VideoDetail = new("/video/detail", HttpMethod.Post, q => "https://music.163.com/weapi/cloudvideo/v1/video/detail", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取视频标签下的视频
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider VideoGroup = new("/video/group", HttpMethod.Post, q => "https://music.163.com/weapi/videotimeline/videogroup/get", [
            new("groupId") { KeyForwarding = "id" },
            new("offset", ParameterType.Optional, "0"),
            new("needUrl", ParameterType.Constant, "true"),
            new("resolution", ParameterType.Optional, "1080") { KeyForwarding = "res" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取视频标签列表
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider VideoGroupList = new("/video/group/list", HttpMethod.Post, q => "https://music.163.com/api/cloudvideo/group/list", [], BuildOptions("weapi"));

    /// <summary>
    /// 收藏视频
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider VideoSub = new("/video/sub", HttpMethod.Post, q => $"https://music.163.com/weapi/cloudvideo/video/{(q["t"] == "1" ? "sub" : "unsub")}", [
            new("id")
        ], BuildOptions("weapi"));

    /// <summary>
    /// 获取视频播放地址
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider VideoUrl = new("/video/url", HttpMethod.Post, q => "https://music.163.com/weapi/cloudvideo/playurl", [
            new("ids") { KeyForwarding = "id", Transformer = JsonArrayTransformer2 },
            new("resolution", ParameterType.Optional, "1080") { KeyForwarding = "res" }
        ], BuildOptions("weapi"));

    /// <summary>
    /// 操作记录 （无文档）
    /// </summary>
    public static readonly NeteaseCloudMusicApiProvider Weblog = new("/weblog", HttpMethod.Post, q => "https://music.163.com/weapi/feedback/weblog", [], BuildOptions("weapi"));

    private static Options BuildOptions(string crypto) => BuildOptions(crypto, null);

    private static Options BuildOptions(string crypto, IEnumerable<Cookie> cookies) => BuildOptions(crypto, cookies, null);

    private static Options BuildOptions(string crypto, IEnumerable<Cookie> cookies, string ua) => BuildOptions(crypto, cookies, ua, null);

    private static Options BuildOptions(string crypto, IEnumerable<Cookie> cookies, string ua, string url)
    {
        CookieCollection cookieCollection;
        Options options;

        cookieCollection = [];
        if (cookies is not null)
        {
            foreach (var cookie in cookies)
            {
                cookieCollection.Add(cookie);
            }
        }

        options = new Options
        {
            crypto = crypto,
            cookie = cookieCollection,
            ua = ua,
            url = url
        };
        return options;
    }

    private static string JsonArrayTransformer(string value) => "[" + value.Replace(" ", string.Empty) + "]";

    private static string JsonArrayTransformer2(string value) => "[\"" + value.Replace(" ", string.Empty) + "\"]";

    private static string BannerTypeTransformer(string type)
    {
        return type switch
        {
            "0" => "pc",
            "1" => "android",
            "2" => "iphone",
            "3" => "ipad",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static string CommentTypeTransformer(string type)
    {
        return type switch
        {
            "0" => "R_SO_4_",// 歌曲
            "1" => "R_MV_5_",// MV
            "2" => "A_PL_0_",// 歌单
            "3" => "R_AL_3_",// 专辑
            "4" => "A_DJ_1_",// 电台
            "5" => "R_VI_62_",// 视频
            "6" => "A_EV_2_",// 动态
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static string DjToplistTypeTransformer(string type)
    {
        return type switch
        {
            "new" => "0",
            "hot" => "1",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static string ResourceTypeTransformer(string type)
    {
        return type switch
        {
            "1" => "R_MV_5_",// MV
            "4" => "A_DJ_1_",// 电台
            "5" => "R_VI_62_",// 视频
            "6" => "A_EV_2_",// 动态
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static string TopListIdTransformer(string idx)
    {
        return idx switch
        {
            "0" => "3779629",// 云音乐新歌榜
            "1" => "3778678",// 云音乐热歌榜
            "2" => "2884035",// 云音乐原创榜
            "3" => "19723756",// 云音乐飙升榜
            "4" => "10520166",// 云音乐电音榜
            "5" => "180106",// UK排行榜周榜
            "6" => "60198",// 美国Billboard周榜
            "7" => "21845217",// KTV嗨榜
            "8" => "11641012",// iTunes榜
            "9" => "120001",// Hit FM Top榜
            "10" => "60131",// 日本Oricon周榜
            "11" => "3733003",// 韩国Melon排行榜周榜
            "12" => "60255",// 韩国Mnet排行榜周榜
            "13" => "46772709",// 韩国Melon原声周榜
            "14" => "112504",// 中国TOP排行榜(港台榜)
            "15" => "64016",// 中国TOP排行榜(内地榜)
            "16" => "10169002",// 香港电台中文歌曲龙虎榜
            "17" => "4395559",// 华语金曲榜
            "18" => "1899724",// 中国嘻哈榜
            "19" => "27135204",// 法国 NRJ EuroHot 30周榜
            "20" => "112463",// 台湾Hito排行榜
            "21" => "3812895",// Beatport全球电子舞曲榜
            "22" => "71385702",// 云音乐ACG音乐榜
            "23" => "991319590",//云音乐说唱榜
            "24" => "71384707",//云音乐古典音乐榜
            "25" => "1978921795",//云音乐电音榜
            "26" => "2250011882",//抖音排行榜
            "27" => "2617766278",//新声榜
            "28" => "745956260",// 云音乐韩语榜
            "29" => "2023401535",// 英国Q杂志中文版周榜
            "30" => "2006508653",// 电竞音乐榜
            "31" => "2809513713",// 云音乐欧美热歌榜
            "32" => "2809577409",// 云音乐欧美新歌榜
            "33" => "2847251561",// 说唱TOP榜
            "34" => "3001835560",// 云音乐ACG动画榜
            "35" => "3001795926",// 云音乐ACG游戏榜
            "36" => "3001890046",// 云音乐ACG VOCALOID榜
            _ => throw new ArgumentOutOfRangeException(nameof(idx)),
        };
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex MyRegex();
}
