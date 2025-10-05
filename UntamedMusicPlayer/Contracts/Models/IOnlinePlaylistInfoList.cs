using System.Collections.ObjectModel;

namespace UntamedMusicPlayer.Contracts.Models;

public abstract class IOnlinePlaylistInfoList : ObservableCollection<IBriefOnlinePlaylistInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
