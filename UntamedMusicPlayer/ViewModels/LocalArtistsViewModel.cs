using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public partial class LocalArtistsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    private List<LocalArtistInfo> _artistList = [.. Data.MusicLibrary.Artists.Values];

    public List<string> SortBy { get; set; } = [.. "Artists_SortBy".GetLocalized().Split(", ")];

    public List<GroupInfoList> GroupedArtistList { get; set; } = [];

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;

    partial void OnSortModeChanged(byte value)
    {
        SortByStr = SortBy[value];
        SaveSortModeAsync();
    }

    [ObservableProperty]
    public partial string SortByStr { get; set; } = "";

    public LocalArtistsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        LoadModeAndArtistList();
    }

    public void Receive(HaveMusicMessage message)
    {
        LoadModeAndArtistList();
    }

    public async void LoadModeAndArtistList()
    {
        _artistList = [.. Data.MusicLibrary.Artists.Values];
        if (_artistList.Count == 0)
        {
            return;
        }
        await LoadSortModeAsync();
        await SortArtists();
        OnPropertyChanged(nameof(GroupedArtistList));
        IsProgressRingActive = false;
    }

    public async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs _)
    {
        var currentsortmode = SortMode;
        SortMode = (byte)(sender as ListView)!.SelectedIndex;
        if (SortMode != currentsortmode)
        {
            IsProgressRingActive = true;
            await SortArtists();
            OnPropertyChanged(nameof(GroupedArtistList));
            IsProgressRingActive = false;
        }
    }

    public void SortByListView_Loaded(object sender, RoutedEventArgs _)
    {
        (sender as ListView)!.SelectedIndex = SortMode;
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

    public void PlayButton_Click(LocalArtistInfo info)
    {
        var songList = Data.MusicLibrary.GetSongsByArtist(info);
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void PlayNextButton_Click(LocalArtistInfo info)
    {
        var songList = Data.MusicLibrary.GetSongsByArtist(info);
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    public void AddToPlayQueueButton_Click(LocalArtistInfo info)
    {
        var songList = Data.MusicLibrary.GetSongsByArtist(info);
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
        }
    }

    public async void AddToPlaylistButton_Click(LocalArtistInfo info, PlaylistInfo playlist)
    {
        var songList = Data.MusicLibrary.GetSongsByArtist(info);
        await Data.PlaylistLibrary.AddToPlaylist(playlist, songList);
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("ArtistSortMode");
        SortByStr = SortBy[SortMode];
    }

    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("ArtistSortMode", SortMode);
    }

    public double GetArtistGridViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }

    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}
