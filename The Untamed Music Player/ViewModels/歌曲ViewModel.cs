using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class 歌曲ViewModel : INotifyPropertyChanged
{
    private readonly ILocalSettingsService _localSettingsService;
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

    private List<string> _sortBy = [.. "歌曲_SortBy".GetLocalized().Split(", ")];
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

    private List<BriefMusicInfo> _songList = Data.MusicLibrary.Musics;

    public List<BriefMusicInfo> SongList
    {
        get => _songList;
        set => _songList = value;
    }

    private ObservableCollection<GroupInfoList> _groupedSongList = [];
    public ObservableCollection<GroupInfoList> GroupedSongList
    {
        get => _groupedSongList;
        set => _groupedSongList = value;
    }

    private ObservableCollection<BriefMusicInfo> _notGroupedSongList = [];
    public ObservableCollection<BriefMusicInfo> NotGroupedSongList
    {
        get => _notGroupedSongList;
        set => _notGroupedSongList = value;
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

    public 歌曲ViewModel(ILocalSettingsService localSettingsService)
    {
        Data.歌曲ViewModel = this;
        _localSettingsService = localSettingsService;
        LoadModeAndSongList();
    }

    public async void LoadModeAndSongList()
    {
        await LoadSortModeAsync();
        await LoadGenreModeAsync();
        await FilterSongs();
        OnPropertyChanged(nameof(GroupedSongList));
        OnPropertyChanged(nameof(NotGroupedSongList));
        OnPropertyChanged(nameof(Genres));
        IsProgressRingActive = false;
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
                await SortSongs();
                OnPropertyChanged(nameof(GroupedSongList));
                OnPropertyChanged(nameof(NotGroupedSongList));
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
                await FilterSongs();
                OnPropertyChanged(nameof(GroupedSongList));
                OnPropertyChanged(nameof(NotGroupedSongList));
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

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList("Songs:All", ConvertGroupedToFlatList(), SortMode);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetPlayList("Songs:All", ConvertGroupedToFlatList(), SortMode);
        if (sender is Button button && button.DataContext is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByPath(briefMusicInfo.Path);
        }
    }

    public double GetSongListViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }

    public void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
    }

    public void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
    }

    public double GetZoomedOutViewGridWidth(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => 69,
            _ => 424
        };
    }

    public int GetZoomedOutViewGridMaxColumn(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => 8,
            _ => 3
        };
    }

    public async Task SortSongs()
    {
        var sortTask = SortMode switch
        {
            0 => SortSongsByTitleAscending(),
            1 => SortSongsByTitleDescending(),
            2 => SortSongsByArtistAscending(),
            3 => SortSongsByArtistDescending(),
            4 => SortSongsByAlbumAscending(),
            5 => SortSongsByAlbumDescending(),
            6 => SortSongsByYearAscending(),
            7 => SortSongsByYearDescending(),
            8 => SortSongsByModifiedTimeAscending(),
            9 => SortSongsByModifiedTimeDescending(),
            10 => SortSongsByFolderAscending(),
            11 => SortSongsByFolderDescending(),
            _ => SortSongsByTitleAscending()
        };

        await sortTask;
    }

    private void SetGroupMode()
    {
        GroupMode = SortMode switch
        {
            0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 or 10 or 11 => true,
            _ => false
        };
    }

    public async Task FilterSongs()
    {
        GroupedSongList = new ObservableCollection<GroupInfoList>(SongList
            .GroupBy(m => TitleComparer.GetGroupKey(m.Title[0]))
            .Select(g => new GroupInfoList(g) { Key = g.Key }));
        NotGroupedSongList = new ObservableCollection<BriefMusicInfo>(SongList);

        if (GenreMode == 0)
        {
            await SortSongs();
            return;
        }

        var genreToFilter = Genres[GenreMode];

        var filterGroupedTask = Task.Run(() =>
        {
            // 过滤GroupedSongList
            foreach (var group in GroupedSongList)
            {
                var filteredItems = group.Where(item => item is BriefMusicInfo musicInfo && musicInfo.GenreStr == genreToFilter).ToList();
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
            var filteredSongs = NotGroupedSongList.Where(musicInfo => musicInfo.GenreStr == genreToFilter).ToList();
            NotGroupedSongList.Clear();
            foreach (var song in filteredSongs)
            {
                NotGroupedSongList.Add(song);
            }
        });
        await Task.WhenAll(filterGroupedTask, filterNotGroupedTask);
        await SortSongs();
    }

    public ObservableCollection<BriefMusicInfo> ConvertGroupedToFlatList()
    {
        if (GroupMode)
        {
            var flatList = new ObservableCollection<BriefMusicInfo>();
            foreach (var group in GroupedSongList)
            {
                foreach (var item in group)
                {
                    if (item is BriefMusicInfo musicInfo)
                    {
                        flatList.Add(musicInfo);
                    }
                }
            }
            return flatList;
        }
        else
        {
            return NotGroupedSongList;
        }
    }

    public object GetSongListViewSource(ICollectionView grouped, ObservableCollection<BriefMusicInfo> notgrouped)
    {
        return GroupMode ? grouped : NotGroupedSongList;
    }

    /// <summary>
    /// 根据歌曲名升序排序
    /// </summary>
    public async Task SortSongsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
               .SelectMany(group => group)
               .OfType<BriefMusicInfo>()
               .OrderBy(m => m.Title, new TitleComparer())
               .GroupBy(m => TitleComparer.GetGroupKey(m.Title[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据歌曲名降序排序
    /// </summary>
    public async Task SortSongsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderByDescending(m => m.Title, new TitleComparer())
                .GroupBy(m => TitleComparer.GetGroupKey(m.Title[0]))
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据艺术家名升序排序
    /// </summary>
    public async Task SortSongsByArtistAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderBy(m => m, new MusicArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据艺术家名降序排序
    /// </summary>
    public async Task SortSongsByArtistDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderByDescending(m => m, new MusicArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据专辑名升序排序
    /// </summary>
    public async Task SortSongsByAlbumAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderBy(m => m, new MusicAlbumComparer())
                .GroupBy(m => m.Album)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据专辑名降序排序
    /// </summary>
    public async Task SortSongsByAlbumDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderByDescending(m => m, new MusicAlbumComparer())
                .GroupBy(m => m.Album)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据发行年份升序排序
    /// </summary>
    public async Task SortSongsByYearAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderBy(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : m.Year.ToString())
                .Select(g => new GroupInfoList(g) { Key = g.Key });
            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据发行年份降序排序
    /// </summary>
    public async Task SortSongsByYearDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderByDescending(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : m.Year.ToString())
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据修改日期升序排序
    /// </summary>
    public async Task SortSongsByModifiedTimeAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedSongList
                .OrderBy(m => m.ModifiedDate);

            NotGroupedSongList = new ObservableCollection<BriefMusicInfo>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据修改日期降序排序
    /// </summary>
    public async Task SortSongsByModifiedTimeDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedSongList
                .OrderByDescending(m => m.ModifiedDate);

            NotGroupedSongList = new ObservableCollection<BriefMusicInfo>(sortedGroups);
        });
    }

    /// <summary>
    /// 根据文件夹排序
    /// </summary>
    /// <returns></returns>
    public async Task SortSongsByFolderAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderBy(m => m, new MusicFolderComparer())
                .GroupBy(m => m.Folder)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    public async Task SortSongsByFolderDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedSongList
                .SelectMany(group => group)
                .OfType<BriefMusicInfo>()
                .OrderByDescending(m => m, new MusicFolderComparer())
                .GroupBy(m => m.Folder)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("SortMode");
    }
    public async Task LoadGenreModeAsync()
    {
        GenreMode = await _localSettingsService.ReadSettingAsync<int>("GenreMode");
    }
    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("SortMode", SortMode);
    }
    public async void SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("GenreMode", GenreMode);
    }
}
