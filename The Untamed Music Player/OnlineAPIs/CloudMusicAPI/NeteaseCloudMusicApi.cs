#pragma warning disable

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Extensions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.util;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
/// <summary>
/// 网易云音乐API
/// </summary>
public sealed partial class NeteaseCloudMusicApi : IDisposable
{
    private readonly HttpClient _client;
    private readonly HttpClientHandler _clientHandler;
    private bool _isDisposed;

    private static readonly Dictionary<string, string> _emptyQueries = [];

    /// <summary />
    public HttpClient Client => _client;

    /// <summary />
    public HttpClientHandler ClientHandler => _clientHandler;

    /// <summary>
    /// 代理服务器
    /// </summary>
    public IWebProxy Proxy
    {
        get => _clientHandler.Proxy;
        set => _clientHandler.Proxy = value;
    }

    /// <summary>
    /// 空请求参数，用于填充 queries 参数
    /// </summary>
    public static Dictionary<string, string> EmptyQueries => _emptyQueries;

    /// <summary>
    /// 构造器
    /// </summary>
    public NeteaseCloudMusicApi()
    {
        _clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = true
        };
        _client = new HttpClient(_clientHandler);
    }

    /// <summary>
    /// API请求（如果.NET版本支持，请使用值元组异步版本 <see cref="RequestAsync(NeteaseCloudMusicApiProvider, Dictionary{string, string})"/>）
    /// </summary>
    /// <param name="provider">API提供者</param>
    /// <param name="queries">参数</param>
    /// <param name="result">请求结果</param>
    /// <returns></returns>
    public bool Request(NeteaseCloudMusicApiProvider provider, Dictionary<string, string> queries, out JsonObject result)
    {
        bool isOk;

        (isOk, result) = RequestAsync(provider, queries).Result;
        return isOk;
    }

    /// <summary>
    /// API请求
    /// </summary>
    /// <param name="provider">API提供者</param>
    /// <param name="queries">参数</param>
    /// <returns></returns>
    public Task<(bool, JsonObject)> RequestAsync(NeteaseCloudMusicApiProvider provider, Dictionary<string, string> queries)
    {
        ArgumentNullException.ThrowIfNull(provider);

        ArgumentNullException.ThrowIfNull(queries);

        if (provider == CloudMusicApiProviders.CheckMusic)
        {
            return HandleCheckMusicAsync(queries);
        }
        else if (provider == CloudMusicApiProviders.Login)
        {
            return HandleLoginAsync(queries);
        }
        else if (provider == CloudMusicApiProviders.LoginStatus)
        {
            return HandleLoginStatusAsync();
        }
        else if (provider == CloudMusicApiProviders.RelatedPlaylist)
        {
            return HandleRelatedPlaylistAsync(queries);
        }

        return RequestAsync(provider.Method, provider.Url(queries), provider.Data(queries), provider.Options);
    }

    private async Task<(bool, JsonObject)> RequestAsync(HttpMethod method, string url, IEnumerable<KeyValuePair<string, string>> data, options options)
    {
        ArgumentNullException.ThrowIfNull(method);

        ArgumentNullException.ThrowIfNull(url);

        ArgumentNullException.ThrowIfNull(data);

        ArgumentNullException.ThrowIfNull(options);

        bool isOk;
        JsonObject json;

        (isOk, json) = await request.createRequest(_client, method, url, data, options);
        json = (JsonObject)json["body"];
        if (!isOk && (int?)json["code"] == 301)
        {
            json["msg"] = "需要登录";
        }

        return (isOk, json);
    }

    private async Task<(bool, JsonObject)> HandleCheckMusicAsync(Dictionary<string, string> queries)
    {
        NeteaseCloudMusicApiProvider provider;
        bool isOk;
        JsonObject json;
        JsonObject result;
        bool playable;

        provider = CloudMusicApiProviders.CheckMusic;
        (isOk, json) = await RequestAsync(provider.Method, provider.Url(queries), provider.Data(queries), provider.Options);
        if (!isOk)
        {
            return (false, null);
        }

        playable = (int?)json["code"] == 200 &&
           json["data"] is JsonArray dataArray &&
           dataArray.Count > 0 &&
           (int?)dataArray[0]?["code"] == 200;
        result = new JsonObject {
                { "success", playable },
                { "message", playable ? "ok" : "亲爱的,暂无版权"}
            };
        return (true, result);
    }

    private async Task<(bool, JsonObject)> HandleLoginAsync(Dictionary<string, string> queries)
    {
        NeteaseCloudMusicApiProvider provider;
        bool isOk;
        JsonObject json;

        provider = CloudMusicApiProviders.Login;
        (isOk, json) = await RequestAsync(provider.Method, provider.Url(queries), provider.Data(queries), provider.Options);
        if (!isOk)
        {
            return (false, null);
        }

        if ((int?)json["code"] == 502)
        {
            json = new JsonObject {
                    { "msg", "账号或密码错误" },
                    { "code", 502 },
                    { "message", "账号或密码错误" }
                };
        }

        return (isOk, json);
    }

    private async Task<(bool, JsonObject)> HandleLoginStatusAsync()
    {
        HttpResponseMessage response;

        response = null;
        try
        {
            const string GUSER = "GUser=";
            const string GBINDS = "GBinds=";

            string s;
            int index;
            JsonObject json;

            response = await _client.GetAsync("https://music.163.com");
            s = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
            index = s.IndexOf(GUSER, StringComparison.Ordinal);
            if (index == -1)
            {
                goto errorExit;
            }

            json = new JsonObject {
                    { "code", 200 }
                };
            var profileJson = JsonNode.Parse(s[(index + GUSER.Length)..]);
            if (profileJson != null)
            {
                json.Add("profile", profileJson.AsObject());
            }

            index = s.IndexOf(GBINDS, StringComparison.Ordinal);
            if (index == -1)
            {
                goto errorExit;
            }

            var bindingsJson = JsonNode.Parse(s[(index + GBINDS.Length)..]);
            if (bindingsJson != null)
            {
                json.Add("bindings", bindingsJson.AsArray());
            }

            return (true, json);
        }
        catch
        {
            goto errorExit;
        }
        finally
        {
            response?.Dispose();
        }
    errorExit:
        return (false, new JsonObject {
                { "code", 301 }
            });
    }

    private async Task<(bool, JsonObject)> HandleRelatedPlaylistAsync(Dictionary<string, string> queries)
    {
        HttpResponseMessage response;

        response = null;
        try
        {
            string s;
            MatchCollection matchs;
            JsonArray playlists;

            response = await _client.SendAsync(HttpMethod.Get, "https://music.163.com/playlist", new QueryCollection { { "id", queries["id"] } }, new QueryCollection { { "User-Agent", request.chooseUserAgent("pc") } });
            s = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
            matchs = MyRegex().Matches(s);
            playlists = new JsonArray();
            matchs.Cast<Match>().Select(match => new JsonObject {
                { "creator", new JsonObject {
                    { "userId", match.Groups[4].Value["/user/home?id=".Length..] },
                    { "nickname", match.Groups[5].Value }
                } },
                { "coverImgUrl", match.Groups[1].Value[..^"?param=50y50".Length] },
                { "name", match.Groups[3].Value },
                { "id", match.Groups[2].Value["/playlist?id=".Length..] },
            }).ToList().ForEach(obj => playlists.Add(obj));

            return (true, new JsonObject {
                    { "code", 200 },
                    { "playlists", playlists }
                });
        }
        catch (Exception ex)
        {
            return (false, new JsonObject {
                    { "code", 500 },
                    { "msg", ex.ToFullString() }
                });
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _clientHandler.Dispose();
        _client.Dispose();
        _isDisposed = true;
    }

    [GeneratedRegex(@"<div class=""cver u-cover u-cover-3"">[\s\S]*?<img src=""([^""]+)"">[\s\S]*?<a class=""sname f-fs1 s-fc0"" href=""([^""]+)""[^>]*>([^<]+?)<\/a>[\s\S]*?<a class=""nm nm f-thide s-fc3"" href=""([^""]+)""[^>]*>([^<]+?)<\/a>")]
    private static partial Regex MyRegex();
}
