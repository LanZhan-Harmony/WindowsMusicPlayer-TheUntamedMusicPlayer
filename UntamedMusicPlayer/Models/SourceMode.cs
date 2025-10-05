using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI;

namespace UntamedMusicPlayer.Models;

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
