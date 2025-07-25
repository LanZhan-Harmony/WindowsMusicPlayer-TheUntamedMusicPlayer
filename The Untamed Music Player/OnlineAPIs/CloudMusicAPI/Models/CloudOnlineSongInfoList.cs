using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

public partial class CloudOnlineSongInfoList : IBriefOnlineSongInfoList
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
