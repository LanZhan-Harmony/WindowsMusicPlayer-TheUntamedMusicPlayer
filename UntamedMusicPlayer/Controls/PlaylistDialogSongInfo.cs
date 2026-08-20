using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

namespace UntamedMusicPlayer.Controls;

public sealed class TempSongInfo(BriefLocalSongInfo originalSong)
{
    public string TrackStr { get; set; } = originalSong.TrackStr;
    public string Title { get; set; } = originalSong.Title;
    public string ArtistsStr { get; set; } =
        originalSong.ArtistsStr == "SongInfo_UnknownArtist".GetLocalized()
            ? ""
            : originalSong.ArtistsStr;
    public BriefLocalSongInfo OriginalSong { get; } = originalSong;
}

public sealed class DisplaySongInfo(IBriefSongInfoBase song)
{
    public string Type { get; set; } =
        song switch
        {
            BriefLocalSongInfo => "DisplaySongInfo_SourceMode0".GetLocalized(),
            BriefUnknownSongInfo => "DisplaySongInfo_SourceMode1".GetLocalized(),
            BriefCloudOnlineSongInfo => "DisplaySongInfo_SourceMode2".GetLocalized(),
            _ => "",
        };

    public IBriefSongInfoBase Song { get; set; } = song;
}
