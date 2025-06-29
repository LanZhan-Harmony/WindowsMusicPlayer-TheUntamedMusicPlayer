using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;

public partial class CloudBriefOnlineMusicInfoList : IBriefOnlineMusicInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int SongCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedSongIDs = [];

    public CloudBriefOnlineMusicInfoList() { }

    public new void Add(IBriefOnlineMusicInfo? info)
    {
        ListCount++;
        if (info is not null && info.IsPlayAvailable && SearchedSongIDs.Add(info.ID))
        {
            base.Add(info);
        }
        if (ListCount == SongCount)
        {
            HasAllLoaded = true;
        }
    }
}
