using System.Collections.ObjectModel;

namespace UntamedMusicPlayer.Contracts.Models;

public abstract class IOnlineAlbumInfoList : ObservableCollection<IBriefOnlineAlbumInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
