using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class LocalSongsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    /// <summary>
    /// 是否分组
    /// </summary>
    private bool _isGrouped = true;

    /// <summary>
    /// 备用歌曲列表
    /// </summary>
    private List<BriefLocalSongInfo> _songList = null!;

    /// <summary>
    /// 排序方式列表
    /// </summary>
    public List<string> SortBy { get; set; } = [.. "Songs_SortBy".GetLocalized().Split(", ")];

    /// <summary>
    /// 分组的歌曲列表
    /// </summary>
    public List<GroupInfoList> GroupedSongList { get; set; } = [];

    /// <summary>
    /// 未分组的歌曲列表
    /// </summary>
    public List<BriefLocalSongInfo> NotGroupedSongList { get; set; } = [];

    /// <summary>
    /// 流派列表
    /// </summary>
    public List<string> Genres { get; set; } = null!;

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
        SortByStr = SortBy[value];
        SetGroupMode();
        SaveSortModeAsync();
    }

    /// <summary>
    /// 当前选择的排序方式字符串
    /// </summary>
    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

    /// <summary>
    /// 流派筛选方式
    /// </summary>
    [ObservableProperty]
    public partial int GenreMode { get; set; } = 0;

    partial void OnGenreModeChanged(int value)
    {
        if (Genres.Count > 0 && value < Genres.Count)
        {
            GenreStr = Genres[value];
        }
        SaveGenreModeAsync();
    }

    /// <summary>
    /// 当前选择的流派字符串
    /// </summary>
    [ObservableProperty]
    public partial string GenreStr { get; set; } = "";

    public LocalSongsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        LoadModeAndSongList();
        Data.LocalSongsViewModel = this;
    }

    public void Receive(HaveMusicMessage message)
    {
        LoadModeAndSongList();
    }

    public async void LoadModeAndSongList()
    {
        _songList = [.. Data.MusicLibrary.Songs];
        if (_songList.Count == 0)
        {
            return;
        }
        Genres = Data.MusicLibrary.Genres;
        await LoadSortModeAsync();
        await LoadGenreModeAsync();
        try
        {
            await FilterSongs();
            OnPropertyChanged(nameof(GroupedSongList));
            OnPropertyChanged(nameof(NotGroupedSongList));
            OnPropertyChanged(nameof(Genres));
            Messenger.Send(new ScrollToSongMessage());
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        finally
        {
            IsProgressRingActive = false;
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
            _ => SortSongsByTitleAscending(),
        };

        await sortTask;
    }

    private void SetGroupMode()
    {
        _isGrouped = SortMode switch
        {
            0 or 1 or 2 or 3 or 4 or 5 or 6 or 7 or 10 or 11 => true,
            _ => false,
        };
    }

    /// <summary>
    /// 过滤歌曲
    /// </summary>
    /// <returns></returns>
    public async Task FilterSongs()
    {
        GroupedSongList =
        [
            .. _songList
                .AsValueEnumerable()
                .GroupBy(m => TitleComparer.GetGroupKey(m.Title[0]))
                .Select(g => new GroupInfoList(g) { Key = g.Key }),
        ];
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
                var filteredItems = group
                    .AsValueEnumerable()
                    .Where(item =>
                        item is BriefLocalSongInfo songInfo && songInfo.GenreStr == genreToFilter
                    )
                    .ToArray();
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
            var filteredSongs = NotGroupedSongList
                .AsValueEnumerable()
                .Where(songInfo => songInfo.GenreStr == genreToFilter)
                .ToArray();
            NotGroupedSongList.Clear();
            foreach (var song in filteredSongs)
            {
                NotGroupedSongList.Add(song);
            }
        });
        await Task.WhenAll(filterGroupedTask, filterNotGroupedTask);
        await SortSongs();
    }

    private List<BriefLocalSongInfo> ConvertGroupedToFlatList()
    {
        return _isGrouped
            ?
            [
                .. GroupedSongList
                    .AsValueEnumerable()
                    .SelectMany(group => group.OfType<BriefLocalSongInfo>()),
            ]
            : NotGroupedSongList;
    }

    public object GetSongListViewSource(
        ICollectionView grouped,
        List<BriefLocalSongInfo> notgrouped
    )
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
                .OrderBy(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : $"{m.Year}")
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
                .OrderByDescending(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : $"{m.Year}")
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
            var sortedGroups = NotGroupedSongList.AsValueEnumerable().OrderBy(m => m.ModifiedDate);

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
                .AsValueEnumerable()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
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
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<BriefLocalSongInfo>()
                .OrderByDescending(m => m, new MusicFolderComparer())
                .GroupBy(m => m.Folder)
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedSongList = [.. sortedGroups];
        });
    }

    public async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentsortmode = SortMode;
        SortMode = (byte)(sender as ListView)!.SelectedIndex;
        if (SortMode != currentsortmode)
        {
            IsProgressRingActive = true;
            await SortSongs();
            OnPropertyChanged(nameof(GroupedSongList));
            OnPropertyChanged(nameof(NotGroupedSongList));
            Messenger.Send(new ScrollToSongMessage());
            IsProgressRingActive = false;
        }
    }

    public void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as ListView)!.SelectedIndex = SortMode;
    }

    public async void GenreListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentGenreMode = GenreMode;
        GenreMode = (byte)(sender as ListView)!.SelectedIndex;
        if (GenreMode != currentGenreMode)
        {
            IsProgressRingActive = true;
            await FilterSongs();
            OnPropertyChanged(nameof(GroupedSongList));
            OnPropertyChanged(nameof(NotGroupedSongList));
            Messenger.Send(new ScrollToSongMessage());
            IsProgressRingActive = false;
        }
    }

    public void GenreListView_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as ListView)!.SelectedIndex = GenreMode;
    }

    public void ShuffledPlayAllButton_Click(object sender, RoutedEventArgs e)
    {
        Data.MusicPlayer.SetShuffledPlayQueue("ShuffledLocalSongs:All", ConvertGroupedToFlatList());
        Data.MusicPlayer.PlaySongByIndexedInfo(Data.MusicPlayer.ShuffledPlayQueue[0]);
    }

    public void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:All:{SortByStr}", ConvertGroupedToFlatList());
        if (e.ClickedItem is BriefLocalSongInfo info)
        {
            Data.MusicPlayer.PlaySongByInfo(info);
        }
    }

    public void PlayButton_Click(BriefLocalSongInfo info)
    {
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:All:{SortByStr}", ConvertGroupedToFlatList());
        Data.MusicPlayer.PlaySongByInfo(info);
    }

    public void PlayNextButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToNextPlay(info);
        }
    }

    /// <summary>
    /// 添加歌曲到播放队列
    /// </summary>
    public void AddToPlayQueueButton_Click(BriefLocalSongInfo info)
    {
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            Data.MusicPlayer.SetPlayQueue("LocalSongs:Part", list);
            Data.MusicPlayer.PlaySongByInfo(info);
        }
        else
        {
            Data.MusicPlayer.AddSongToPlayQueue(info);
        }
    }

    public async void AddToPlaylistButton_Click(BriefLocalSongInfo info, PlaylistInfo playlist)
    {
        await Data.PlaylistLibrary.AddToPlaylist(playlist, info);
    }

    public void ShowAlbumButton_Click(BriefLocalSongInfo info)
    {
        var localAlbumInfo = Data.MusicLibrary.GetAlbumInfoBySong(info.Album);
        if (localAlbumInfo is not null)
        {
            Data.SelectedLocalAlbum = localAlbumInfo;
            Data.ShellPage!.Navigate(
                nameof(LocalAlbumDetailPage),
                "",
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    public void ShowArtistButton_Click(BriefLocalSongInfo info)
    {
        var localArtistInfo = Data.MusicLibrary.GetArtistInfoBySong(info.Artists[0]);
        if (localArtistInfo is not null)
        {
            Data.SelectedLocalArtist = localArtistInfo;
            Data.ShellPage!.Navigate(
                nameof(LocalArtistDetailPage),
                "",
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("SortMode");
        SortByStr = SortBy[SortMode];
    }

    public async Task LoadGenreModeAsync()
    {
        GenreMode = await _localSettingsService.ReadSettingAsync<int>("GenreMode");
        if (Genres.Count > 0 && GenreMode < Genres.Count)
        {
            GenreStr = Genres[GenreMode];
        }
    }

    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("SortMode", SortMode);
    }

    public async void SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("GenreMode", GenreMode);
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
            _ => 426,
        };
    }

    public Thickness GetZoomedOutViewTextBlockMargin(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => new Thickness(0, 0, 0, 0),
            _ => new Thickness(15, 0, 15, 0),
        };
    }

    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}
