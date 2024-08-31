using System.Collections.ObjectModel;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class 专辑ViewModel
{
    private ObservableCollection<AlbumInfo> _albumList = [];
    public ObservableCollection<AlbumInfo> AlbumList
    {
        get => _albumList;
        set => _albumList = value;
    }

    public 专辑ViewModel()
    {
        LoadAlbumList(Data.MusicLibrary.Albums);
    }

    public async void LoadAlbumList(Dictionary<string, AlbumInfo> albumList)
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(albumList.Values);
            SortAlbumsByModifiedTimeDescending();
        });
    }

    public void SortAlbumsByTitle()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Name));
        GC.Collect();
    }

    public void SortAlbumsByTitleDescending()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Name));
        GC.Collect();
    }

    public void SortAlbumsByYear()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Year));
        GC.Collect();
    }

    public void SortAlbumsByYearDescending()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Year));
        GC.Collect();
    }

    public void SortAlbumsByArtist()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Artist));
        GC.Collect();
    }

    public void SortAlbumsByArtistDescending()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Artist));
        GC.Collect();
    }

    public void SortAlbumsByModifiedTime()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.ModifiedDate));
        GC.Collect();
    }

    public void SortAlbumsByModifiedTimeDescending()
    {
        AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.ModifiedDate));
        GC.Collect();
    }
}
