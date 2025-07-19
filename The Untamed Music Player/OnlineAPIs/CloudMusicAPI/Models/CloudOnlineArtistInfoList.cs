using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Models;

public partial class CloudOnlineArtistInfoList : IOnlineArtistInfoList
{
    public const byte Limit = 30;
    public ushort Page { get; set; } = 0;
    public int ArtistCount { get; set; } = 0;
    public int ListCount { get; set; } = 0;
    public readonly HashSet<long> SearchedArtistIDs = [];

    public CloudOnlineArtistInfoList() { }

    public new void Add(IBriefOnlineArtistInfo? info)
    {
        ListCount++;
        if (info is not null && SearchedArtistIDs.Add(info.ID))
        {
            base.Add(info);
        }
        if (ListCount == ArtistCount)
        {
            HasAllLoaded = true;
        }
    }
}
