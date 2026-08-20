using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Core.Models;

public enum SourceMode
{
    Null = -1,
    Local = 0,
    Unknown = 1,
    Netease = 2,
}

public static class SourceModeHelper
{
    public static SourceMode GetSourceMode(IBriefSongInfoBase? info)
    {
        return info switch
        {
            BriefLocalSongInfo => SourceMode.Local,
            BriefUnknownSongInfo => SourceMode.Unknown,
            BriefCloudOnlineSongInfo => SourceMode.Netease,
            _ => SourceMode.Null,
        };
    }
}
