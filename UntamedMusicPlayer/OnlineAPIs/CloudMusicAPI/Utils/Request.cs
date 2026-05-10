#pragma warning disable IL2026, IL3050
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Extensions;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Utils;

internal static partial class Request
{
    private static readonly string[] userAgentList =
    [
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 EdgiOS/134.3124.77 Mobile/15E148 Safari/605.1.15",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/134.0.6998.99 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Linux; Android 16; AGT-AN00; HMSCore 6.14.0.309; GMSCore 25.45.34) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.5735.196 HuaweiBrowser/16.0.9.303 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 16; Pixel 3 XL) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.6998.135 Mobile Safari/537.36 EdgA/134.0.3124.68",
        "Mozilla/5.0 (Linux; Android 16; SM-G973F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.6998.135 Mobile Safari/537.36 EdgA/134.0.3124.68",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 10_3_2 like Mac OS X) AppleWebKit/603.2.4 (KHTML, like Gecko) Mobile/14F89;GameHelper",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_6_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:136.0) Gecko/20100101 Firefox/136.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Safari/605.1.15",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0",
    ];

    [GeneratedRegex(@"\w*api", RegexOptions.Compiled)]
    private static partial Regex ApiTypeRegex();

    [GeneratedRegex(@"\s*Domain=[^(;|$)]+;*", RegexOptions.Compiled)]
    private static partial Regex CookieDomainRegex();

    public static string ChooseUserAgent(string? ua)
    {
        return ua switch
        {
            "mobile" => userAgentList[Random.Shared.Next(8)],
            "pc" => userAgentList[Random.Shared.Next(6) + 8],
            _ => string.IsNullOrEmpty(ua)
                ? userAgentList[Random.Shared.Next(userAgentList.Length)]
                : ua,
        };
    }

    public static async Task<(bool, JsonObject)> CreateRequest(
        HttpClient client,
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>> data_,
        Options options
    )
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(data_);
        ArgumentNullException.ThrowIfNull(options);

        var headers = new Dictionary<string, string>(3)
        {
            ["User-Agent"] = ChooseUserAgent(options.UA),
            ["Cookie"] = BuildCookieHeader(options.Cookie),
        };

        if (method == HttpMethod.Post)
        {
            headers["Content-Type"] = "application/x-www-form-urlencoded";
        }

        if (url.Contains("music.163.com", StringComparison.Ordinal))
        {
            headers["Referer"] = "https://music.163.com";
        }

        var data = data_ is ICollection<KeyValuePair<string, string>> dataCollection
            ? new Dictionary<string, string>(dataCollection.Count)
            : [];
        foreach (var item in data_)
        {
            data.Add(item.Key, item.Value);
        }

        switch (options.Crypto)
        {
            case "weapi":
                data["csrf_token"] = options.Cookie["__csrf"]?.Value ?? "";
                data = Crypto.WEApi(data);
                url = ApiTypeRegex().Replace(url, "weapi");
                break;
            case "linuxapi":
                data = Crypto.LinuxApi(
                    new Dictionary<string, object>
                    {
                        { "method", method.Method },
                        { "url", ApiTypeRegex().Replace(url, "api") },
                        { "params", data },
                    }
                );
                headers["User-Agent"] =
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";
                url = "https://music.163.com/api/linux/forward";
                break;
            case "eapi":
            {
                var cookie = new CookieCollection();
                foreach (Cookie item in options.Cookie)
                {
                    cookie.Add(new Cookie(item.Name, item.Value));
                }

                var csrfToken = cookie["__csrf"]?.Value ?? "";
                var header = new Dictionary<string, string>(12)
                {
                    { "osver", cookie["osver"]?.Value ?? "" }, // 系统版本
                    { "deviceId", cookie["deviceId"]?.Value ?? "" }, // encrypt.base64.encode(imei + '\t02:00:00:00:00:00\t5106025eb79a5247\t70ffbaac7')
                    { "appver", cookie["appver"]?.Value ?? "6.1.1" }, // app版本
                    { "versioncode", cookie["versioncode"]?.Value ?? "140" }, // 版本号
                    { "mobilename", cookie["mobilename"]?.Value ?? "" }, // 设备model
                    {
                        "buildver",
                        cookie["buildver"]?.Value ?? $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                    },
                    { "resolution", cookie["resolution"]?.Value ?? "1920x1080" }, // 设备分辨率
                    { "__csrf", csrfToken },
                    { "os", cookie["os"]?.Value ?? "android" },
                    { "channel", cookie["channel"]?.Value ?? "" },
                    {
                        "requestId",
                        $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Random.Shared.Next(1000):D4}"
                    },
                };
                if (cookie["MUSIC_U"] is Cookie musicUCookie)
                {
                    header["MUSIC_U"] = musicUCookie.Value;
                }

                if (cookie["MUSIC_A"] is Cookie musicACookie)
                {
                    header["MUSIC_A"] = musicACookie.Value;
                }

                headers["Cookie"] = BuildCookieHeader(header);
                data["header"] = JsonSerializer.Serialize(header);
                data = Crypto.EApi(options.Url ?? "", data);
                url = ApiTypeRegex().Replace(url, "eapi");
                break;
            }
        }

        var answer = new JsonObject
        {
            { "status", 500 },
            { "body", null },
            { "cookie", null },
        };

        HttpResponseMessage? response = null;
        try
        {
            var statusCode = 500;

            response = await client.SendAsync(
                method,
                url,
                null,
                headers,
                data.ToQueryString(),
                "application/x-www-form-urlencoded"
            );
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException();
            }

            if (!response.Headers.TryGetValues("set-cookie", out var responseCookies))
            {
                responseCookies = [];
            }

            var cookieArray = new JsonArray();
            foreach (var rawCookie in responseCookies)
            {
                var cookieValue = CookieDomainRegex().Replace(rawCookie, "");
                if (!string.IsNullOrEmpty(cookieValue))
                {
                    cookieArray.Add(JsonValue.Create(cookieValue));
                }
            }
            answer["cookie"] = cookieArray;

            if (options.Crypto == "eapi")
            {
                byte[] buffer;
                try
                {
                    using var stream = new DeflateStream(
                        await response.Content.ReadAsStreamAsync(),
                        CompressionMode.Decompress
                    );
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    buffer = ms.ToArray();
                }
                catch
                {
                    buffer = await response.Content.ReadAsByteArrayAsync();
                }

                try
                {
                    answer["body"] = JsonObject.Parse(
                        Encoding.UTF8.GetString(Crypto.Decrypt(buffer))
                    );
                }
                catch
                {
                    answer["body"] = JsonObject.Parse(Encoding.UTF8.GetString(buffer));
                }

                statusCode =
                    answer["body"] is JsonObject eapiBody
                    && eapiBody["code"] is JsonValue eapiCode
                    && eapiCode.TryGetValue<int>(out var eapiCodeInt)
                        ? eapiCodeInt
                        : (int)response.StatusCode;
            }
            else
            {
                answer["body"] = JsonObject.Parse(await response.Content.ReadAsStringAsync());
                statusCode =
                    answer["body"] is JsonObject body
                    && body["code"] is JsonValue code
                    && code.TryGetValue<int>(out var codeInt)
                        ? codeInt
                        : (int)response.StatusCode;
                if (statusCode == 502)
                {
                    statusCode = 200;
                }
            }

            statusCode = 100 < statusCode && statusCode < 600 ? statusCode : 400;
            answer["status"] = statusCode;
            return (statusCode == 200, answer);
        }
        catch (Exception ex)
        {
            answer["status"] = 502;
            answer["body"] = new JsonObject { { "code", 502 }, { "msg", ex.ToFullString() } };
            return (false, answer);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static string BuildCookieHeader(CookieCollection cookies)
    {
        var sb = new StringBuilder();
        foreach (Cookie cookie in cookies)
        {
            if (sb.Length > 0)
            {
                sb.Append("; ");
            }

            sb.Append(Uri.EscapeDataString(cookie.Name));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(cookie.Value));
        }

        return sb.ToString();
    }

    private static string BuildCookieHeader(Dictionary<string, string> cookies)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in cookies)
        {
            if (sb.Length > 0)
            {
                sb.Append("; ");
            }

            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }

        return sb.ToString();
    }
}
