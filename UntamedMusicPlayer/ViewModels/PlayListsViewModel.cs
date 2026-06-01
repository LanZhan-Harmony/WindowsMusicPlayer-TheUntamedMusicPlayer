using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using ZLinq;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class PlayListsViewModel
    : ObservableRecipient,
        IRecipient<HavePlaylistMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    private List<PlaylistInfo> _tempPlaylists = App.GetService<PlaylistLibrary>().Playlists;

    public ObservableCollection<PlaylistInfo> Playlists { get; set; } = [];

    public PlaylistInfo? LastNavigatedPlaylist { get; set; }

    [ObservableProperty]
    public partial bool IsMainProgressRingActive { get; set; } = !App.GetService<PlaylistLibrary>().HasLoaded;

    [ObservableProperty]
    public partial Visibility NoPlaylistControlVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility HavePlaylistControlVisibility { get; set; } = Visibility.Collapsed;

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

    public PlayListsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        _ = LoadModeAndPlayList();
    }

    public void Receive(HavePlaylistMessage message)
    {
        IsMainProgressRingActive = false;
        NoPlaylistControlVisibility = message.HasPlaylist
            ? Visibility.Collapsed
            : Visibility.Visible;
        HavePlaylistControlVisibility = message.HasPlaylist
            ? Visibility.Visible
            : Visibility.Collapsed;
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
            NoPlaylistControlVisibility = Visibility.Visible;
            HavePlaylistControlVisibility = Visibility.Collapsed;
            return;
        }
        await LoadSortModeAsync();
        await SortPlaylists();
        OnPropertyChanged(nameof(Playlists));
        NoPlaylistControlVisibility =
            Playlists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HavePlaylistControlVisibility =
            Playlists.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
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

    public void SortByListView_Loaded(object sender, RoutedEventArgs _)
    {
        (sender as ListView)!.SelectedIndex = SortMode;
    }

    public async void SortByListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs _unused
    )
    {
        _ = ChangeSortModeAsync((sender as ListView)!.SelectedIndex);
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
        Data.PlayQueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public void PlayNextButton(PlaylistInfo info)

    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
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
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"Songs:Playlist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
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

