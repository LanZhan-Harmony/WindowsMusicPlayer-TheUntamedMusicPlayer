using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using UntamedMusicPlayer.Core.Contracts.Services;
using UntamedMusicPlayer.Core.Helpers;
using UntamedMusicPlayer.Core.Messages;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Playback;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class PlayListsViewModel
    : ObservableRecipient,
        IRecipient<HavePlaylistMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly MusicPlayer _musicPlayer;

    private List<PlaylistInfo> _tempPlaylists = App.GetService<PlaylistLibrary>().Playlists;

    public ObservableCollection<PlaylistInfo> Playlists { get; set; } = [];

    public PlaylistInfo? LastNavigatedPlaylist { get; set; }

    [ObservableProperty]
    public partial bool IsMainProgressRingActive { get; set; } =
        !App.GetService<PlaylistLibrary>().HasLoaded;

    [ObservableProperty]
    public partial bool IsNoPlaylistControlVisible { get; set; } = false;

    [ObservableProperty]
    public partial bool IsHavePlaylistControlVisible { get; set; } = false;

    public List<string> SortBy { get; set; } = [.. "Playlists_SortBy".GetLocalized().Split(", ")];

    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;

    partial void OnSortModeChanged(byte value)
    {
        SortByStr = SortBy[value];
        _ = SaveSortModeAsync();
    }

    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = false;

    public PlayListsViewModel(MusicPlayer musicPlayer)
        : base(StrongReferenceMessenger.Default)
    {
        _musicPlayer = musicPlayer;
        Messenger.Register(this);
        _ = LoadModeAndPlayList();
    }

    public void Receive(HavePlaylistMessage message)
    {
        IsMainProgressRingActive = false;
        IsNoPlaylistControlVisible = !message.HasPlaylist;
        IsHavePlaylistControlVisible = message.HasPlaylist;
        _ = LoadModeAndPlayList();
    }

    public async Task LoadModeAndPlayList()
    {
        if (IsMainProgressRingActive)
        {
            return;
        }
        _tempPlaylists = App.GetService<PlaylistLibrary>().Playlists;
        if (_tempPlaylists.Count == 0)
        {
            IsNoPlaylistControlVisible = true;
            IsHavePlaylistControlVisible = false;
            return;
        }
        await LoadSortModeAsync();
        await SortPlaylists();
        OnPropertyChanged(nameof(Playlists));
        IsNoPlaylistControlVisible = Playlists.Count == 0;
        IsHavePlaylistControlVisible = Playlists.Count > 0;
    }

    public async Task SortPlaylists()
    {
        var sortTask = SortMode switch
        {
            0 => SortPlaylistsByTitleAscending(),
            1 => SortPlaylistsByTitleDescending(),
            2 => SortPlaylistsByModifiedTimeAscending(),
            3 => SortPlaylistsByModifiedTimeDescending(),
            _ => SortPlaylistsByTitleAscending(),
        };
        await sortTask;
    }

    private async Task SortPlaylistsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var templist = _tempPlaylists
                .AsValueEnumerable()
                .OrderBy(p => p.Name, new TitleComparer());
            Playlists = [.. templist];
        });
    }

    private async Task SortPlaylistsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var templist = _tempPlaylists
                .AsValueEnumerable()
                .OrderByDescending(p => p.Name, new TitleComparer());
            Playlists = [.. templist];
        });
    }

    private async Task SortPlaylistsByModifiedTimeAscending()
    {
        await Task.Run(() =>
        {
            var templist = _tempPlaylists.AsValueEnumerable().OrderBy(p => p.ModifiedDate);
            Playlists = [.. templist];
        });
    }

    private async Task SortPlaylistsByModifiedTimeDescending()
    {
        await Task.Run(() =>
        {
            var templist = _tempPlaylists
                .AsValueEnumerable()
                .OrderByDescending(p => p.ModifiedDate);
            Playlists = [.. templist];
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
        await SortPlaylists();
        OnPropertyChanged(nameof(Playlists));
        IsProgressRingActive = false;
    }

    [RelayCommand]
    public void PlayButton(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        _musicPlayer.QueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
        _musicPlayer.PlaySongByInfo(songList[0]);
    }

    [RelayCommand]
    public void PlayNextButton(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]
    public void AddToPlayQueueButton(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        if (_musicPlayer.QueueManager.CurrentQueue.Count == 0)
        {
            _musicPlayer.QueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
            _musicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            _musicPlayer.QueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]
    public async Task AddToPlaylistButton(Tuple<PlaylistInfo, PlaylistInfo> tuple)
    {
        var (info, playlist) = tuple;
        var songList = info.GetAllSongs();
        await App.GetService<PlaylistLibrary>().AddToPlaylist(playlist, songList);
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("PlaylistSortMode");
        SortByStr = SortBy[SortMode];
    }

    public async Task SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("PlaylistSortMode", SortMode);
    }

    public void Dispose() => Messenger.Unregister<HavePlaylistMessage>(this);
}
