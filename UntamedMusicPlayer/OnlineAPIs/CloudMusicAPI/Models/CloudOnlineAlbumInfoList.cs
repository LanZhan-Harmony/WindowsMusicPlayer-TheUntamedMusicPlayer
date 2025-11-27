using UntamedMusicPlayer.Contracts.Models;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Models;

public sealed partial class CloudOnlineAlbumInfoList : IOnlineAlbumInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int AlbumCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedAlbumIDs = [];

    public CloudOnlineAlbumInfoList() { }

    public new void Add(IBriefOnlineAlbumInfo? info)
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
