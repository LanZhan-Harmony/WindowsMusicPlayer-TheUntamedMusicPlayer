using System.Net;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Utils;

internal sealed class Options
{
    public string Crypto { get; set; } = "";
    public CookieCollection Cookie { get; set; } = [];
    public string? UA { get; set; }
    public string? Url { get; set; }
}
