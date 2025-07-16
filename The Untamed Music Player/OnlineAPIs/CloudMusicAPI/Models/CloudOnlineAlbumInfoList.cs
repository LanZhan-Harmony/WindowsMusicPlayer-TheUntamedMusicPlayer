using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public class CloudOnlineAlbumInfoList : IOnlineAlbumInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int AlbumCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedAlbumIDs = [];

    public CloudOnlineAlbumInfoList() { }

    public new void Add(IOnlineAlbumInfo? info)
    {
        ListCount++;
        if (info is not null && SearchedAlbumIDs.Add(info.ID))
        {
            base.Add(info);
        }
        if (ListCount == AlbumCount)
        {
            HasAllLoaded = true;
        }
    }
}
