using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class LocalArtistsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly MusicPlayer _musicPlayer;

    private List<LocalArtistInfo> _artistList = [.. App.GetService<MusicLibrary>().Artists.Values];

    public List<string> SortBy { get; set; } = [.. "Artists_SortBy".GetLocalized().Split(", ")];

    public List<GroupInfoList> GroupedArtistList { get; set; } = [];

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;

    partial void OnSortModeChanged(byte value)
    {
        SortByStr = SortBy[value];
        _ = SaveSortModeAsync();
    }

    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

    public LocalArtistsViewModel(MusicPlayer musicPlayer)
        : base(StrongReferenceMessenger.Default)
    {
        _musicPlayer = musicPlayer;
        Messenger.Register(this);
        _ = LoadModeAndArtistList();
    }

    public void Receive(HaveMusicMessage message)
    {
        _ = LoadModeAndArtistList();
    }

    public async Task LoadModeAndArtistList()
    {
        _artistList = [.. App.GetService<MusicLibrary>().Artists.Values];
        if (_artistList.Count == 0)
        {
            return;
        }
        await LoadSortModeAsync();
        await SortArtists();
        OnPropertyChanged(nameof(GroupedArtistList));
        IsProgressRingActive = false;
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
        await SortArtists();
        OnPropertyChanged(nameof(GroupedArtistList));
        IsProgressRingActive = false;
    }

    public async Task SortArtists()
    {
        var sortTask = SortMode switch
        {
            0 => SortArtistsByTitleAscending(),
            1 => SortArtistsByTitleDescending(),
            _ => SortArtistsByTitleAscending(),
        };

        await sortTask;
    }

    public async Task SortArtistsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = _artistList
                .AsValueEnumerable()
                .OrderBy(m => m.Name, new ArtistTitleComparer())
                .GroupBy(m =>
                    m.Name == "SongInfo_UnknownArtist".GetLocalized()
                        ? "..."
                        : TitleComparer.GetGroupKey(m.Name[0])
                )
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = [.. sortedGroups];
        });
    }

    public async Task SortArtistsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = _artistList
                .AsValueEnumerable()
                .OrderByDescending(m => m.Name, new ArtistTitleComparer())
                .GroupBy(m =>
                    m.Name == "SongInfo_UnknownArtist".GetLocalized()
                        ? "..."
                        : TitleComparer.GetGroupKey(m.Name[0])
                )
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = [.. sortedGroups];
        });
    }

    [RelayCommand]
    public void PlayButton(LocalArtistInfo info)
    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        _musicPlayer.QueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    public void PlayNextButton(LocalArtistInfo info)
    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Artist:{info.Name}",
                songList
            );
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]
    public void AddToPlayQueueButton(LocalArtistInfo info)
    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue(
                $"LocalSongs:Artist:{info.Name}",
                songList
            );
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToEnd(songList);
        }
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(Tuple<LocalArtistInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("ArtistSortMode");
        SortByStr = SortBy[SortMode];
    }

    public async Task SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("ArtistSortMode", SortMode);
    }

    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}
