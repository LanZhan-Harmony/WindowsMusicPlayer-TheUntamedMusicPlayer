using System.Collections.ObjectModel;

namespace The_Untamed_Music_Player.Contracts.Models;

public abstract class IOnlineArtistInfoList : ObservableCollection<IBriefOnlineArtistInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
