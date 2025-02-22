using System.Collections.Concurrent;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;
public partial class LocalSongsViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();

    public double ScrollViewerVerticalOffset
    {
        get;
        set
        {
            field = value;
            SaveScrollViewerVerticalOffsetAsync();
        }
    } = 0;

    /// <summary>
    /// 是否分组
    /// </summary>
    private bool _isGrouped = true;

    /// <summary>
    /// 备用歌曲列表
    /// </summary>
    private readonly ConcurrentBag<BriefMusicInfo> _songList = Data.MusicLibrary.Songs;

    /// <summary>
    /// 排序方式列表
    /// </summary>
    public List<string> SortBy { get; set; } = [.. "LocalSongs_SortBy".GetLocalized().Split(", ")];

    /// <summary>
    /// 分组的歌曲列表
    /// </summary>
    public List<GroupInfoList> GroupedSongList { get; set; } = [];

    /// <summary>
    /// 未分组的歌曲列表
    /// </summary>
    public List<BriefMusicInfo> NotGroupedSongList { get; set; } = [];

    /// <summary>
    /// 流派列表
    /// </summary>
    public List<string> Genres { get; set; } = Data.MusicLibrary.Genres;

    /// <summary>
    /// 是否显示加载进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    /// <summary>
    /// 排序方式, 0: 标题升序, 1: 标题降序, 2: 艺术家升序, 3: 艺术家降序, 4: 专辑升序, 5: 专辑降序, 6: 年份升序, 7: 年份降序, 8: 修改日期升序, 9: 修改日期降序, 10: 文件夹升序, 11: 文件夹降序
    /// </summary>
    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;
    partial void OnSortModeChanged(byte value)
    {
        SetGroupMode();
        SaveSortModeAsync();
    }

    /// <summary>
    /// 流派筛选方式
    /// </summary>
    [ObservableProperty]
    public partial int GenreMode { get; set; } = 0;
    partial void OnGenreModeChanged(int value)
    {
        SaveGenreModeAsync();
    }

    public LocalSongsViewModel()
    {
        LoadScrollViewerVerticalOffsetAsync();
        LoadModeAndSongList();
        Data.LocalSongsViewModel = this;
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
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

    private void MusicLibrary_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "LibraryReloaded")
        {
            LoadModeAndSongList();
        }
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
        _isGrouped = SortMode switch
        {
            0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 or 10 or 11 => true,
            _ => false
        };
    }

    /// <summary>
    /// 过滤歌曲
    /// </summary>
    /// <returns></returns>
    public async Task FilterSongs()
    {
        GroupedSongList = [.. _songList
            .GroupBy(m => TitleComparer.GetGroupKey(m.Title[0]))
            .Select(g => new GroupInfoList(g) { Key = g.Key })];
        NotGroupedSongList = [.. _songList];

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

    private List<BriefMusicInfo> ConvertGroupedToFlatList()
    {
        return _isGrouped
            ? [.. GroupedSongList.SelectMany(group => group.OfType<BriefMusicInfo>())]
            : NotGroupedSongList;
    }

    public object GetSongListViewSource(ICollectionView grouped, List<BriefMusicInfo> notgrouped)
    {
        return _isGrouped ? grouped : NotGroupedSongList;
    }

    /// <summary>
    /// 根据歌曲名升序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据歌曲名降序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据艺术家名升序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据艺术家名降序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据专辑名升序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据专辑名降序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据发行年份升序排序
    /// </summary>
    /// <returns></returns>
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
            GroupedSongList = [.. sortedGroups];
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

            GroupedSongList = [.. sortedGroups];
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

            NotGroupedSongList = [.. sortedGroups];
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

            NotGroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据文件夹升序排序
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

            GroupedSongList = [.. sortedGroups];
        });
    }

    /// <summary>
    /// 根据文件夹降序排序
    /// </summary>
    /// <returns></returns>
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

            GroupedSongList = [.. sortedGroups];
        });
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

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayList("LocalSongs:All", ConvertGroupedToFlatList(), 0, SortMode);
        if (e.ClickedItem is BriefMusicInfo briefMusicInfo)
        {
            Data.MusicPlayer.PlaySongByInfo(briefMusicInfo);
        }
    }

    public void PlayButton_Click(BriefMusicInfo info)
    {
        Data.MusicPlayer.SetPlayList("LocalSongs:All", ConvertGroupedToFlatList(), 0, SortMode);
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(BriefMusicInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefMusicInfo> { info };
            Data.MusicPlayer.SetPlayList("LocalSongs:Part", list
                , 0, 0);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    public void ShowAlbumButton_Click(BriefMusicInfo info)
    {

    }

    public void ShowArtistButton_Click(BriefMusicInfo info)
    {

    }

    public async void LoadScrollViewerVerticalOffsetAsync()
    {
        ScrollViewerVerticalOffset = await _localSettingsService.ReadSettingAsync<double>("LocalSongsScrollViewerVerticalOffset");
    }
    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("SortMode");
    }
    public async Task LoadGenreModeAsync()
    {
        GenreMode = await _localSettingsService.ReadSettingAsync<int>("GenreMode");
    }
    public async void SaveScrollViewerVerticalOffsetAsync()
    {
        await _localSettingsService.SaveSettingAsync("LocalSongsScrollViewerVerticalOffset", ScrollViewerVerticalOffset);
    }
    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("SortMode", SortMode);
    }
    public async void SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("GenreMode", GenreMode);
    }

    public string GetSortByStr(byte SortMode)
    {
        return SortBy[SortMode];
    }

    public string GetGenreStr(int GenreMode)
    {
        return Genres[GenreMode];
    }

    public double GetSongListViewOpacity(bool isActive)
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
}
