using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class LocalAlbumsViewModel : INotifyPropertyChanged
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool _isProgressRingActive = true;
    public bool IsProgressRingActive
    {
        get => _isProgressRingActive;
        set
        {
            _isProgressRingActive = value;
            OnPropertyChanged(nameof(IsProgressRingActive));
        }
    }

    private List<string> _sortBy = [.. "LocalAlbums_SortBy".GetLocalized().Split(", ")];
    public List<string> SortBy
    {
        get => _sortBy;
        set => _sortBy = value;
    }

    private byte _sortMode;
    public byte SortMode
    {
        get => _sortMode;
        set
        {
            _sortMode = value;
            SetGroupMode();
            OnPropertyChanged(nameof(SortMode));
            SaveSortModeAsync();
        }
    }

    private bool _groupMode;
    public bool GroupMode
    {
        get => _groupMode;
        set => _groupMode = value;
    }

    private List<AlbumInfo> _albumList = [.. Data.MusicLibrary.Albums.Values];
    public List<AlbumInfo> AlbumList
    {
        get => _albumList;
        set => _albumList = value;
    }

    private ObservableCollection<GroupInfoList> _groupedAlbumList = [];
    public ObservableCollection<GroupInfoList> GroupedAlbumList
    {
        get => _groupedAlbumList;
        set => _groupedAlbumList = value;
    }

    private ObservableCollection<AlbumInfo> _notGroupedAlbumList = [];

    public ObservableCollection<AlbumInfo> NotGroupedAlbumList
    {
        get => _notGroupedAlbumList;
        set => _notGroupedAlbumList = value;
    }

    private int _genreMode;
    public int GenreMode
    {
        get => _genreMode;
        set
        {
            _genreMode = value;
            OnPropertyChanged(nameof(GenreMode));
            SaveGenreModeAsync();
        }
    }

    private ObservableCollection<string> _genres = Data.MusicLibrary.Genres;
    public ObservableCollection<string> Genres
    {
        get => _genres;
        set => _genres = value;
    }

    public LocalAlbumsViewModel()
    {
        LoadModeAndAlbumList();
        Data.LocalAlbumsViewModel = this;
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
    }

    public async void LoadModeAndAlbumList()
    {
        await LoadSortModeAsync();
        await LoadGenreModeAsync();
        await FilterAlbums();
        OnPropertyChanged(nameof(GroupedAlbumList));
        OnPropertyChanged(nameof(NotGroupedAlbumList));
        OnPropertyChanged(nameof(Genres));
        IsProgressRingActive = false;
    }

    private void MusicLibrary_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == "LibraryReloaded"))
        {
            LoadModeAndAlbumList();
        }
    }

    public async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentsortmode = SortMode;
        if (sender is ListView listView && listView.SelectedIndex is int selectedIndex)
        {
            SortMode = (byte)selectedIndex;
            if (SortMode != currentsortmode)
            {
                IsProgressRingActive = true;
                await SortAlbums();
                OnPropertyChanged(nameof(GroupedAlbumList));
                OnPropertyChanged(nameof(NotGroupedAlbumList));
                IsProgressRingActive = false;
            }
        }
    }

    public string GetSortByStr(byte SortMode)
    {
        return SortBy[SortMode];
    }

    public void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.SelectedIndex = SortMode;
        }
    }

    public async void GenreListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentGenreMode = GenreMode;
        if (sender is ListView listView && listView.SelectedIndex is int selectedIndex)
        {
            GenreMode = selectedIndex;
            if (GenreMode != currentGenreMode)
            {
                IsProgressRingActive = true;
                await FilterAlbums();
                OnPropertyChanged(nameof(GroupedAlbumList));
                OnPropertyChanged(nameof(NotGroupedAlbumList));
                IsProgressRingActive = false;
            }
        }
    }

    public void GenreListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.SelectedIndex = GenreMode;
        }
    }

    public string GetGenreStr(int GenreMode)
    {
        return Genres[GenreMode];
    }

    public double GetAlbumGridViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }

    public double GetZoomedOutViewGridWidth(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => 71,
            _ => 426
        };
    }

    public Thickness GetZoomedOutViewTextBlockMargin(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => new Thickness(0, 0, 0, 0),
            _ => new Thickness(15, 0, 15, 0)
        };
    }

    public async Task SortAlbums()
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

    private void SetGroupMode()
    {
        GroupMode = SortMode switch
        {
            0 or 1 or 2 or 3 or 4 or 5 => true,
            _ => false
        };
    }

    public async Task FilterAlbums()
    {
        GroupedAlbumList = [.. AlbumList
            .GroupBy(m => TitleComparer.GetGroupKey(m.Name[0]))
            .Select(g => new GroupInfoList(g) { Key = g.Key })];
        NotGroupedAlbumList = [.. AlbumList];

        if (GenreMode == 0)
        {
            await SortAlbums();
            return;
        }

        var genreToFilter = Genres[GenreMode];

        var filterGroupedTask = Task.Run(() =>
        {
            // 过滤GroupedSongList
            foreach (var group in GroupedAlbumList)
            {
                var filteredItems = group.Where(item => item is AlbumInfo albumInfo && albumInfo.GenreStr == genreToFilter).ToList();
                group.Clear();
                foreach (var item in filteredItems)
                {
                    group.Add(item);
                }
            }
        });
        var filterNotGroupedTask = Task.Run(() =>
        {
            // 过滤NotGroupedSongList
            var filteredSongs = NotGroupedAlbumList.Where(albumInfo => albumInfo.GenreStr == genreToFilter).ToList();
            NotGroupedAlbumList.Clear();
            foreach (var song in filteredSongs)
            {
                NotGroupedAlbumList.Add(song);
            }
        });
        await Task.WhenAll(filterGroupedTask, filterNotGroupedTask);
        await SortAlbums();
    }

    public object GetAlbumGridViewSource(ICollectionView grouped, ObservableCollection<AlbumInfo> notgrouped)
    {
        return GroupMode ? grouped : NotGroupedAlbumList;
    }

    public async Task SortAlbumsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
               .SelectMany(group => group)
               .OfType<AlbumInfo>()
               .OrderBy(m => m.Name, new AlbumTitleComparer())
               .GroupBy(m => m.Name == "MusicInfo_UnknownAlbum".GetLocalized() ? "..." : TitleComparer.GetGroupKey(m.Name[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
               .SelectMany(group => group)
               .OfType<AlbumInfo>()
               .OrderByDescending(m => m.Name, new AlbumTitleComparer())
               .GroupBy(m => m.Name == "MusicInfo_UnknownAlbum".GetLocalized() ? "..." : TitleComparer.GetGroupKey(m.Name[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByYearAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .SelectMany(group => group)
                .OfType<AlbumInfo>()
                .OrderBy(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : m.Year.ToString())
                .Select(g => new GroupInfoList(g) { Key = g.Key });
            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByYearDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .SelectMany(group => group)
                .OfType<AlbumInfo>()
                .OrderByDescending(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : m.Year.ToString())
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByArtistAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .SelectMany(group => group)
                .OfType<AlbumInfo>()
                .OrderBy(m => m, new AlbumArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByArtistDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .SelectMany(group => group)
                .OfType<AlbumInfo>()
                .OrderByDescending(m => m, new AlbumArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByModifiedTimeAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedAlbumList
                .OrderBy(m => m.ModifiedDate);

            NotGroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByModifiedTimeDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedAlbumList
                .OrderByDescending(m => m.ModifiedDate);

            NotGroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("AlbumSortMode");
    }
    public async Task LoadGenreModeAsync()
    {
        GenreMode = await _localSettingsService.ReadSettingAsync<int>("AlbumGenreMode");
    }
    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("AlbumSortMode", SortMode);
    }
    public async void SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("AlbumGenreMode", GenreMode);
    }
}
