using System.Collections.ObjectModel;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.ViewModels;

public class 专辑ViewModel
{
    private List<string> _sortBy = [.. "专辑_SortBy".GetLocalized().Split(", ")];
    public List<string> SortBy
    {
        get => _sortBy;
        set => _sortBy = value;
    }
    private byte _sortMode;
    public byte SortMode
    {
        get => _sortMode;
        set => _sortMode = value;
    }

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
        });
        SortAlbums();
    }

    public async void SortAlbums()
    {
        var sortTask = SortMode switch
        {
            0 => SortAlbumsByTitleAscending(),
            1 => SortAlbumsByTitleDescending(),
            2 => SortAlbumsByYearAscending(),
            3 => SortAlbumsByYearDescending(),
            4 => SortAlbumsByArtistAscending(),
            5 => SortAlbumsByArtistDescending(),
            6 => SortAlbumsByModifiedTimeAscending(),
            7 => SortAlbumsByModifiedTimeDescending(),
            _ => SortAlbumsByTitleAscending()
        };

        await sortTask;
    }

    public async Task SortAlbumsByTitleAscending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Name));
        });
    }

    public async Task SortAlbumsByTitleDescending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Name));
        });
    }

    public async Task SortAlbumsByYearAscending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Year));
        });
    }

    public async Task SortAlbumsByYearDescending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Year));
        });
    }

    public async Task SortAlbumsByArtistAscending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.Artist));
        });
    }

    public async Task SortAlbumsByArtistDescending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.Artist));
        });
    }

    public async Task SortAlbumsByModifiedTimeAscending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderBy(x => x.ModifiedDate));
        });
    }

    public async Task SortAlbumsByModifiedTimeDescending()
    {
        await Task.Run(() =>
        {
            AlbumList = new ObservableCollection<AlbumInfo>(AlbumList.OrderByDescending(x => x.ModifiedDate));
        });
    }
}
