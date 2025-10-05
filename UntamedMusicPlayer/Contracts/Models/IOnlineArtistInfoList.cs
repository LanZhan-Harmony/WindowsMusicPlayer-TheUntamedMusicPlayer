using System.Collections.ObjectModel;

namespace UntamedMusicPlayer.Contracts.Models;

public abstract class IOnlineArtistInfoList : ObservableCollection<IBriefOnlineArtistInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
