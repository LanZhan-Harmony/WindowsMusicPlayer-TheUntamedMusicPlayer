using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayListsViewModel
    : ObservableRecipient,
        IRecipient<HavePlaylistMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    private List<PlaylistInfo> _tempPlaylists = Data.PlaylistLibrary.Playlists;

    public ObservableCollection<PlaylistInfo> Playlists { get; set; } = [];

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
        SaveSortModeAsync();
    }

    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = false;

    public PlayListsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        LoadModeAndPlayList();
    }

    public void Receive(HavePlaylistMessage message)
    {
        NoPlaylistControlVisibility = message.HasPlaylist
            ? Visibility.Collapsed
            : Visibility.Visible;
        HavePlaylistControlVisibility = message.HasPlaylist
            ? Visibility.Visible
            : Visibility.Collapsed;
        LoadModeAndPlayList();
    }

    public async void LoadModeAndPlayList()
    {
        _tempPlaylists = Data.PlaylistLibrary.Playlists;
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

    public void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as ListView)!.SelectedIndex = SortMode;
    }

    public async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentsortmode = SortMode;
        SortMode = (byte)(sender as ListView)!.SelectedIndex;
        if (SortMode != currentsortmode)
        {
            IsProgressRingActive = true;
            await SortPlaylists();
            OnPropertyChanged(nameof(Playlists));
            IsProgressRingActive = false;
        }
    }

    public void PlayButton_Click(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void PlayNextButton_Click(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    public void AddToPlayQueueButton_Click(PlaylistInfo info)
    {
        var songList = info.GetAllSongs();
        if (songList.Length == 0)
        {
            return;
        }
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"Songs:Playlist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
    }

    public async void AddToPlaylistButton_Click(PlaylistInfo info, PlaylistInfo playlist)
    {
        var songList = info.GetAllSongs();
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("PlaylistSortMode");
        SortByStr = SortBy[SortMode];
    }

    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("PlaylistSortMode", SortMode);
    }

    public void Dispose() => Messenger.Unregister<HavePlaylistMessage>(this);
}
