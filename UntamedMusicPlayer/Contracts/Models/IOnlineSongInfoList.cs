using System.Collections.ObjectModel;

namespace UntamedMusicPlayer.Contracts.Models;

public abstract class IOnlineSongInfoList : ObservableCollection<IBriefOnlineSongInfo>
{
    public string KeyWords { get; set; } = "";
    public bool HasAllLoaded { get; set; } = false;
}
