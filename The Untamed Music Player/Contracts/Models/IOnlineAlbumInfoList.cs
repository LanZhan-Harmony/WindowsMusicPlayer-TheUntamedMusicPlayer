using System.Collections.ObjectModel;

namespace The_Untamed_Music_Player.Contracts.Models;

public abstract class IOnlineAlbumInfoList : ObservableCollection<IBriefOnlineAlbumInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
