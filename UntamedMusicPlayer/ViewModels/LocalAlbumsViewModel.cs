using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;
using ZLinq;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalAlbumsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly INavigationService _navigationService =
        App.GetService<INavigationService>();

    private bool _groupMode = true;

    private List<LocalAlbumInfo> _albumList = [.. App.GetService<MusicLibrary>().Albums.Values];

    public List<string> SortBy { get; set; } = [.. "Albums_SortBy".GetLocalized().Split(", ")];

    public List<GroupInfoList> GroupedAlbumList { get; set; } = [];

    public List<LocalAlbumInfo> NotGroupedAlbumList { get; set; } = [];

    public List<string> Genres { get; set; } = App.GetService<MusicLibrary>().Genres;

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;

    partial void OnSortModeChanged(byte value)
    {
        SortByStr = SortBy[value];
        SetGroupMode();
        _ = SaveSortModeAsync();
    }

    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

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

    [ObservableProperty]
    public partial string GenreStr { get; set; } = "";

    public LocalAlbumsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        _ = LoadModeAndAlbumList();
    }

    public void Receive(HaveMusicMessage message)
    {
        _ = LoadModeAndAlbumList();
    }

    public async Task LoadModeAndAlbumList()
    {
        _albumList = [.. App.GetService<MusicLibrary>().Albums.Values];
        if (_albumList.Count == 0)
        {
            return;
        }
        Genres = App.GetService<MusicLibrary>().Genres;
        await LoadSortModeAsync();
        await LoadGenreModeAsync();
        await FilterAlbums();
        OnPropertyChanged(nameof(GroupedAlbumList));
        OnPropertyChanged(nameof(NotGroupedAlbumList));
        OnPropertyChanged(nameof(Genres));
        IsProgressRingActive = false;
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
            _ => SortAlbumsByTitleAscending(),
        };

        await sortTask;
    }

    private void SetGroupMode()
    {
        _groupMode = SortMode switch
        {
            0 or 1 or 2 or 3 or 4 or 5 => true,
            _ => false,
        };
    }

    public async Task FilterAlbums()
    {
        GroupedAlbumList =
        [
            .. _albumList
                .AsValueEnumerable()
                .GroupBy(m => TitleComparer.GetGroupKey(m.Name[0]))
                .Select(g => CreateGroupInfoList(g, g.Key)),
        ];
        NotGroupedAlbumList = [.. _albumList];

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
                var filteredItems = group
                    .AsValueEnumerable()
                    .Where(item =>
                        item is LocalAlbumInfo localAlbumInfo
                        && localAlbumInfo.GenreStr == genreToFilter
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
            var filteredSongs = NotGroupedAlbumList
                .AsValueEnumerable()
                .Where(localAlbumInfo => localAlbumInfo.GenreStr == genreToFilter)
                .ToArray();
            NotGroupedAlbumList.Clear();
            foreach (var song in filteredSongs)
            {
                NotGroupedAlbumList.Add(song);
            }
        });
        await Task.WhenAll(filterGroupedTask, filterNotGroupedTask);
        await SortAlbums();
    }

    public ICollectionView GetAlbumGridViewSource(
        ICollectionView grouped,
        List<LocalAlbumInfo> notgrouped
    )
    {
        return _groupMode ? grouped : new CollectionViewSource { Source = notgrouped }.View;
    }

    public async Task SortAlbumsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderBy(m => m.Name, new AlbumTitleComparer())
                .GroupBy(m =>
                    m.Name == "SongInfo_UnknownAlbum".GetLocalized()
                        ? "..."
                        : TitleComparer.GetGroupKey(m.Name[0])
                )
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderByDescending(m => m.Name, new AlbumTitleComparer())
                .GroupBy(m =>
                    m.Name == "SongInfo_UnknownAlbum".GetLocalized()
                        ? "..."
                        : TitleComparer.GetGroupKey(m.Name[0])
                )
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByYearAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderBy(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : $"{m.Year}")
                .Select(g => CreateGroupInfoList(g, g.Key));
            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByYearDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderByDescending(m => m.Year)
                .GroupBy(m => m.Year == 0 ? "..." : $"{m.Year}")
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByArtistAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderBy(m => m, new AlbumArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByArtistDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = GroupedAlbumList
                .AsValueEnumerable()
                .SelectMany(group => group)
                .OfType<LocalAlbumInfo>()
                .OrderByDescending(m => m, new AlbumArtistComparer())
                .GroupBy(m => m.ArtistsStr)
                .Select(g => CreateGroupInfoList(g, g.Key));

            GroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByModifiedTimeAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedAlbumList.AsValueEnumerable().OrderBy(m => m.ModifiedDate);

            NotGroupedAlbumList = [.. sortedGroups];
        });
    }

    public async Task SortAlbumsByModifiedTimeDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = NotGroupedAlbumList
                .AsValueEnumerable()
                .OrderByDescending(m => m.ModifiedDate);

            NotGroupedAlbumList = [.. sortedGroups];
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
        await SortAlbums();
        OnPropertyChanged(nameof(GroupedAlbumList));
        OnPropertyChanged(nameof(NotGroupedAlbumList));
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
        await FilterAlbums();
        OnPropertyChanged(nameof(GroupedAlbumList));
        OnPropertyChanged(nameof(NotGroupedAlbumList));
        IsProgressRingActive = false;
    }

    [RelayCommand]

    public void PlayButton(LocalAlbumInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByAlbum(info);
        App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public void PlayNextButton(LocalAlbumInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByAlbum(info);
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]

    public void AddToPlayQueueButton(LocalAlbumInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByAlbum(info);
        if (App.GetService<MusicPlayer>().QueueManager.CurrentQueue.Count == 0)
        {
            App.GetService<MusicPlayer>().QueueManager.SetNormalPlayQueue($"LocalSongs:Album:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            App.GetService<MusicPlayer>().QueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]

    public async Task AddToPlaylistButton(Tuple<LocalAlbumInfo, PlaylistInfo> tuple)

    {

        var (info, playlist) = tuple;
        var songList = App.GetService<MusicLibrary>().GetSongsByAlbum(info);
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    [RelayCommand]

    public void ShowArtistButton(LocalAlbumInfo info)

    {
        var localArtistInfo = App.GetService<MusicLibrary>().GetArtistInfoBySong(info.Artists[0]);
        if (localArtistInfo is not null)
        {
            _navigationService.NavigateShell(
                nameof(LocalArtistDetailPage),
                new LocalArtistNavigationArgs(localArtistInfo, nameof(LocalAlbumsPage)),
                new SuppressNavigationTransitionInfo()
            );
        }
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("AlbumSortMode");
        SortByStr = SortBy[SortMode];
    }

    public async Task LoadGenreModeAsync()
    {
        GenreMode = await _localSettingsService.ReadSettingAsync<int>("AlbumGenreMode");
        if (Genres.Count > 0 && GenreMode < Genres.Count)
        {
            GenreStr = Genres[GenreMode];
        }
    }

    public async Task SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("AlbumSortMode", SortMode);
    }

    public async Task SaveGenreModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("AlbumGenreMode", GenreMode);
    }

    public Visibility GetAlbumGridViewVisibility(bool isActive)
    {
        return isActive ? Visibility.Collapsed : Visibility.Visible;
    }

    private GroupInfoList CreateGroupInfoList(IEnumerable<object> items, string key)
    {
        return new GroupInfoList(items)
        {
            Key = key,
            ZoomedOutViewGridWidth = GetZoomedOutViewGridWidth(SortMode),
            ZoomedOutViewTextBlockMargin = GetZoomedOutViewTextBlockMargin(SortMode),
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

    private static Thickness GetZoomedOutViewTextBlockMargin(byte sortmode)
    {
        return sortmode switch
        {
            0 or 1 => new Thickness(0, 0, 0, 0),
            _ => new Thickness(15, 0, 15, 0),
        };
    }






    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}

