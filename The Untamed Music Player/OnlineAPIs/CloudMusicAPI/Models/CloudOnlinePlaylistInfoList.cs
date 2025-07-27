using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public partial class CloudOnlinePlaylistInfoList : IOnlinePlaylistInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int PlaylistCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedPlaylistIDs = [];

    public CloudOnlinePlaylistInfoList() { }

    public new void Add(IBriefOnlinePlaylistInfo? info)
    {
        ListCount++;
        if (info is not null && SearchedPlaylistIDs.Add(info.ID))
        {
            base.Add(info);
        }
        if (ListCount == PlaylistCount)
        {
            HasAllLoaded = true;
        }
    }
}
