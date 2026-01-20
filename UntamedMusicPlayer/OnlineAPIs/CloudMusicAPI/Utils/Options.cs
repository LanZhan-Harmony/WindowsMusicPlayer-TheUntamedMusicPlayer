using System.Net;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Utils;

internal sealed class Options
{
    public string Crypto { get; set; } = null!;
    public CookieCollection Cookie { get; set; } = null!;
    public string UA { get; set; } = null!;
    public string Url { get; set; } = null!;
}
