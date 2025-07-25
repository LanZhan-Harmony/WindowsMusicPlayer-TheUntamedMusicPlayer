#pragma warning disable

using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Utils;

internal static partial class Request
{
    private static readonly string[] userAgentList =
    [
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_6_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6.1 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Linux; Android 5.0; SM-G900P Build/LRX21T) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 5.1.1; Nexus 6 Build/LYZ28E) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Mobile Safari/537.36",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 10_3_2 like Mac OS X) AppleWebKit/603.2.4 (KHTML, like Gecko) Mobile/14F89;GameHelper",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_6_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPad; CPU OS 10_0 like Mac OS X) AppleWebKit/602.1.38 (KHTML, like Gecko) Version/10.0 Mobile/14A300 Safari/602.1",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:130.0) Gecko/20100101 Firefox/130.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:130.0) Gecko/20100101 Firefox/130.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0",
    ];

    public static string ChooseUserAgent(string ua)
    {
        return ua switch
        {
            "mobile" => userAgentList[(int)Math.Floor(new Random().NextDouble() * 7)],
            "pc" => userAgentList[(int)Math.Floor(new Random().NextDouble() * 5) + 8],
            _ => string.IsNullOrEmpty(ua)
                ? userAgentList[(int)Math.Floor(new Random().NextDouble() * userAgentList.Length)]
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

        Dictionary<string, string> headers;
        Dictionary<string, string> data;
        JsonObject answer;
        HttpResponseMessage response;

        headers = new Dictionary<string, string>
        {
            ["User-Agent"] = ChooseUserAgent(options.ua),
            ["Cookie"] = string.Join(
                "; ",
                options
                    .cookie.Cast<Cookie>()
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

        data = [];
        foreach (var item in data_)
        {
            data.Add(item.Key, item.Value);
        }

        switch (options.crypto)
        {
            case "weapi":
            {
                data["csrf_token"] = options.cookie["__csrf"]?.Value ?? string.Empty;
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
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36";
                url = "https://music.163.com/api/linux/forward";
                break;
            }
            case "eapi":
            {
                CookieCollection cookie;
                string csrfToken;
                Dictionary<string, string> header;

                cookie = [];
                foreach (Cookie item in options.cookie)
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
                    { "buildver", cookie["buildver"]?.Value ?? $"{GetCurrentTotalSeconds()}" },
                    { "resolution", cookie["resolution"]?.Value ?? "1920x1080" }, // 设备分辨率
                    { "__csrf", csrfToken },
                    { "os", cookie["os"]?.Value ?? "android" },
                    { "channel", cookie["channel"]?.Value ?? string.Empty },
                    {
                        "requestId",
                        $"{GetCurrentTotalMilliseconds()}_{$"{Math.Floor(new Random().NextDouble() * 1000)}".PadLeft(4, '0')}"
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
                data = Crypto.EApi(options.url, data);
                url = MyRegex1().Replace(url, "eapi");
                break;
            }
        }
        answer = new JsonObject
        {
            { "status", 500 },
            { "body", null },
            { "cookie", null },
        };
        response = null;
        try
        {
            IEnumerable<string> temp1;
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

            if (!response.Headers.TryGetValues("set-cookie", out temp1))
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
            if (options.crypto == "eapi")
            {
                DeflateStream stream;
                byte[] buffer;

                stream = null;
                try
                {
                    stream = new DeflateStream(
                        await response.Content.ReadAsStreamAsync(),
                        CompressionMode.Decompress
                    );
                    buffer = ReadStream(stream);
                }
                catch
                {
                    buffer = await response.Content.ReadAsByteArrayAsync();
                }
                finally
                {
                    stream?.Dispose();
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

        ulong GetCurrentTotalSeconds()
        {
            TimeSpan _timeSpan;

            _timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)_timeSpan.TotalSeconds;
        }

        ulong GetCurrentTotalMilliseconds()
        {
            TimeSpan _timeSpan;

            _timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (ulong)_timeSpan.TotalMilliseconds;
        }

        byte[] ReadStream(Stream _stream)
        {
            byte[] _buffer;
            List<byte> _byteList;

            _buffer = new byte[0x1000];
            _byteList = [];
            for (var i = 0; i < int.MaxValue; i++)
            {
                int count;

                count = _stream.Read(_buffer, 0, _buffer.Length);
                if (count == 0x1000)
                {
                    _byteList.AddRange(_buffer);
                }
                else if (count == 0)
                {
                    return [.. _byteList];
                }
                else
                {
                    for (var j = 0; j < count; j++)
                    {
                        _byteList.Add(_buffer[j]);
                    }
                }
            }
            throw new OutOfMemoryException();
        }
    }

    [GeneratedRegex(@"\w*api")]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"\s*Domain=[^(;|$)]+;*")]
    private static partial Regex MyRegex2();
}
