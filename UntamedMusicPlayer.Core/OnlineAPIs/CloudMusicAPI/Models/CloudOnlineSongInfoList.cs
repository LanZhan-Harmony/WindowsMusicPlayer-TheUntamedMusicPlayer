using UntamedMusicPlayer.Core.Contracts.Models;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;

public sealed partial class CloudOnlineSongInfoList : IOnlineSongInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int SongCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedSongIDs = [];

    public CloudOnlineSongInfoList() { }

    public new void Add(IBriefOnlineSongInfo? info)
    {
        ListCount++;
        if (info!.IsPlayAvailable && SearchedSongIDs.Add(info.ID))
        {
            base.Add(info);
        }
        if (ListCount == SongCount)
        {
            HasAllLoaded = true;
        }
    }
}
