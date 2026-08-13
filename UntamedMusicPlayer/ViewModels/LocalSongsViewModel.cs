using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalSongsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService = App.GetService<INavigationService>();
    private readonly ILogger _logger = LoggingService.CreateLogger<LocalSongsViewModel>();
    private readonly MusicPlayer _musicPlayer;

    /// <summary>
    /// 是否分组
    /// </summary>
    private bool _isGrouped = true;

    public bool IsGrouped => _isGrouped;

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
        OnPropertyChanged(nameof(IsGrouped));
        _ = SaveSortModeAsync();
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
        _ = SaveGenreModeAsync();
    }

    /// <summary>
    /// 当前选择的流派字符串
    /// </summary>
    [ObservableProperty]
    public partial string GenreStr { get; set; } = "";

    public LocalSongsViewModel(MusicPlayer musicPlayer)
        : base(StrongReferenceMessenger.Default)
    {
        _musicPlayer = musicPlayer;
        Messenger.Register(this);
        _ = LoadModeAndSongList();
    }

    public void Receive(HaveMusicMessage message)
    {
        _ = LoadModeAndSongList();
    }

    public async Task LoadModeAndSongList()
    {
        _songList = [.. App.GetService<MusicLibrary>().Songs];
        if (_songList.Count == 0)
        {
            return;
        }
        Genres = App.GetService<MusicLibrary>().Genres;
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
            _logger.ZLogError(ex, $"加载音乐时发生错误");
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
                .Select(g => CreateGroupInfoList(g, g.Key)),
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

    private List<BriefLocalSongInfo> ConvertGroupedToFlatList(BriefLocalSongInfo? info = null)
    {
        if (_isGrouped)
        {
            if ((SortMode is 10 or 11) && Settings.IsOnlyAddSpecificFolder)
            {
                return
                [
                    .. GroupedSongList
                        .AsValueEnumerable()
                        .Where(group => group.Key == info?.Folder)
                        .SelectMany(group => group.OfType<BriefLocalSongInfo>()),
                ];
            }
            return
            [
                .. GroupedSongList
                    .AsValueEnumerable()
                    .SelectMany(group => group.OfType<BriefLocalSongInfo>()),
            ];
        }
        else
        {
            return NotGroupedSongList;
        }
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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));
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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

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
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedSongList = [.. sortedGroups];
        });
    }

    public async Task ChangeSortModeAsync(int selectedIndex)
    {
        if (selectedIndex < 0)
        {
            return;
        }

        var currentSortMode = SortMode;
        SortMode = (byte)selectedIndex;
        if (SortMode == currentSortMode)
        {
            return;
        }

        IsProgressRingActive = true;
        await SortSongs();
        OnPropertyChanged(nameof(GroupedSongList));
        OnPropertyChanged(nameof(NotGroupedSongList));
        Messenger.Send(new ScrollToSongMessage());
        IsProgressRingActive = false;
    }

    public async Task ChangeGenreModeAsync(int selectedIndex)
    {
        if (selectedIndex < 0)
        {
            return;
        }

        var currentGenreMode = GenreMode;
        GenreMode = selectedIndex;
        if (GenreMode == currentGenreMode)
        {
            return;
        }

        IsProgressRingActive = true;
        await FilterSongs();
        OnPropertyChanged(nameof(GroupedSongList));
        OnPropertyChanged(nameof(NotGroupedSongList));
        Messenger.Send(new ScrollToSongMessage());
        IsProgressRingActive = false;
    }

    [RelayCommand]
    public void ShuffledPlayAllButton()
    {
        _musicPlayer.QueueManager.SetShuffledPlayQueue(
            "ShuffledLocalSongs:All",
            ConvertGroupedToFlatList()
        );
        _musicPlayer.PlaySongByIndexedInfo(_musicPlayer.QueueManager.CurrentQueue[0]);
    }

    public void SongListView_ItemClick(BriefLocalSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"LocalSongs:All:{SortByStr}",
            ConvertGroupedToFlatList(info)
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void PlayButton(BriefLocalSongInfo info)
    {
        _musicPlayer.QueueManager.SetNormalPlayQueue(
            $"LocalSongs:All:{SortByStr}",
            ConvertGroupedToFlatList(info)
        );
        _musicPlayer.PlaySongByInfo(info);
    }

    [RelayCommand]
    public void PlayNextButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue("LocalSongs:Part", list);
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay([info]);
        }
    }

    /// <summary>
    /// 添加歌曲到播放队列
    /// </summary>
    [RelayCommand]
    public void AddToPlayQueueButton(BriefLocalSongInfo info)
    {
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            var list = new List<BriefLocalSongInfo> { info };
            _musicPlayer.QueueManager.SetNormalPlayQueue("LocalSongs:Part", list);
            _musicPlayer.PlaySongByInfo(info);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd([info]);
        }
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(Tuple<BriefLocalSongInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, info);
    }

    [RelayCommand]
    public void ShowAlbumButton(BriefLocalSongInfo info)
    {
        var localAlbumInfo = App.GetService<MusicLibrary>().GetAlbumInfoBySong(info.Album);
        if (localAlbumInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(LocalAlbumDetailPage),
                new LocalAlbumNavigationArgs(localAlbumInfo, nameof(LocalSongsPage)),
                NavigationTransition.Suppress
            );
        }
    }

    [RelayCommand]
    public void ShowArtistButton(BriefLocalSongInfo info)
    {
        var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(info.Artists[0]);
        if (localArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(LocalArtistDetailPage),
                new LocalArtistNavigationArgs(localArtistInfo, nameof(LocalSongsPage)),
                NavigationTransition.Suppress
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

    public async Task SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("SortMode", SortMode);
    }

    public async Task SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("GenreMode", GenreMode);
    }

    private GroupInfoList CreateGroupInfoList(IEnumerable<object> items, string key)
    {
        return new GroupInfoList(items)
        {
            Key = key,
            ZoomedOutViewGridWidth = GetZoomedOutViewGridWidth(SortMode),
        };
    }

    private static double GetZoomedOutViewGridWidth(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => 71,
            _ => 426,
        };
    }

    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}
