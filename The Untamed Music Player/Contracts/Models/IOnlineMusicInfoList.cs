using System.Collections.ObjectModel;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Contracts.Models;
public abstract class IBriefOnlineMusicInfoList : ObservableCollection<IBriefOnlineMusicInfo>
{
    protected string _keyWords = "";
    public bool HasAllLoaded { get; set; } = false;

    public abstract Task SearchAsync(string keyWords);
    public abstract Task SearchMoreAsync();
    public abstract Task<List<SearchResult>> GetSearchResultAsync(string keyWords);
}