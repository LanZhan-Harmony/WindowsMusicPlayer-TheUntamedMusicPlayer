#pragma warning disable
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

    public static string ChooseUserAgent(string UA)
    {
        return UA switch
        {
            "mobile" => userAgentList[Random.Shared.Next(8)],
            "pc" => userAgentList[Random.Shared.Next(6) + 8],
            _ => string.IsNullOrEmpty(UA)
                ? userAgentList[Random.Shared.Next(userAgentList.Length)]
                : UA,
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

        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = ChooseUserAgent(options.UA),
            ["Cookie"] = string.Join(
                "; ",
                options
                    .Cookie.Cast<Cookie>()
                    .Select(t => Uri.EscapeDataString(t.Name) + "=" + Uri.EscapeDataString(t.Value))
            ),
        };
        if (method == HttpMethod.Post)
        {
            headers["Content-Type"] = "application/x-www-form-urlencoded";
        }

        if (url.Contains("music.163.com"))
        {
            headers["Referer"] = "https://music.163.com";
        }

        var data = new Dictionary<string, string>();
        foreach (var item in data_)
        {
            data.Add(item.Key, item.Value);
        }

        switch (options.Crypto)
        {
            case "weapi":
            {
                data["csrf_token"] = options.Cookie["__csrf"]?.Value ?? string.Empty;
                data = Crypto.WEApi(data);
                url = MyRegex1().Replace(url, "weapi");
                break;
            }
            case "linuxapi":
            {
                data = Crypto.LinuxApi(
                    new Dictionary<string, object>
                    {
                        { "method", method.Method },
                        { "url", MyRegex1().Replace(url, "api") },
                        { "params", data },
                    }
                );
                headers["User-Agent"] =
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";
                url = "https://music.163.com/api/linux/forward";
                break;
            }
            case "eapi":
            {
                CookieCollection cookie;
                string csrfToken;
                Dictionary<string, string> header;

                cookie = [];
                foreach (Cookie item in options.Cookie)
                {
                    cookie.Add(new Cookie(item.Name, item.Value));
                }

                csrfToken = cookie["__csrf"]?.Value ?? string.Empty;
                header = new Dictionary<string, string>()
                {
                    { "osver", cookie["osver"]?.Value ?? string.Empty }, // 系统版本
                    { "deviceId", cookie["deviceId"]?.Value ?? string.Empty }, // encrypt.base64.encode(imei + '\t02:00:00:00:00:00\t5106025eb79a5247\t70ffbaac7')
                    { "appver", cookie["appver"]?.Value ?? "6.1.1" }, // app版本
                    { "versioncode", cookie["versioncode"]?.Value ?? "140" }, // 版本号
                    { "mobilename", cookie["mobilename"]?.Value ?? string.Empty }, // 设备model
                    {
                        "buildver",
                        cookie["buildver"]?.Value ?? $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                    },
                    { "resolution", cookie["resolution"]?.Value ?? "1920x1080" }, // 设备分辨率
                    { "__csrf", csrfToken },
                    { "os", cookie["os"]?.Value ?? "android" },
                    { "channel", cookie["channel"]?.Value ?? string.Empty },
                    {
                        "requestId",
                        $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Random.Shared.Next(1000):D4}"
                    },
                };
                if (cookie["MUSIC_U"] is not null)
                {
                    header["MUSIC_U"] = cookie["MUSIC_U"].Value;
                }

                if (cookie["MUSIC_A"] is not null)
                {
                    header["MUSIC_A"] = cookie["MUSIC_A"].Value;
                }

                headers["Cookie"] = string.Join(
                    "; ",
                    header.Select(t =>
                        Uri.EscapeDataString(t.Key) + "=" + Uri.EscapeDataString(t.Value)
                    )
                );
                data["header"] = JsonSerializer.Serialize(header);
                data = Crypto.EApi(options.Url, data);
                url = MyRegex1().Replace(url, "eapi");
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
            JsonValue temp2;
            int temp3;

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

            if (!response.Headers.TryGetValues("set-cookie", out var temp1))
            {
                temp1 = [];
            }

            var cookieArray = new JsonArray();
            temp1
                .Select(x => MyRegex2().Replace(x, string.Empty))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList()
                .ForEach(x => cookieArray.Add(x));
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
                    temp2 = (JsonValue)answer["body"]["code"];
                    answer["status"] = temp2 is null ? (int)response.StatusCode : (int)temp2;
                }
                catch
                {
                    answer["body"] = JsonObject.Parse(Encoding.UTF8.GetString(buffer));
                    answer["status"] = (int)response.StatusCode;
                }
            }
            else
            {
                answer["body"] = JsonObject.Parse(await response.Content.ReadAsStringAsync());
                temp2 = (JsonValue)answer["body"]["code"];
                answer["status"] = temp2 is null ? (int)response.StatusCode : (int)temp2;
                if (temp2 is not null && (int)temp2 == 502)
                {
                    answer["status"] = 200;
                }
            }
            temp3 = (int)answer["status"];
            temp3 = 100 < temp3 && temp3 < 600 ? temp3 : 400;
            answer["status"] = temp3;
            return (temp3 == 200, answer);
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

    [GeneratedRegex(@"\w*api", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"\s*Domain=[^(;|$)]+;*", RegexOptions.Compiled)]
    private static partial Regex MyRegex2();
}
