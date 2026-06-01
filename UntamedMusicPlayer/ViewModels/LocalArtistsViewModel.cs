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

public sealed partial class LocalArtistsViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

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

    public LocalArtistsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
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
        await SortArtists();
        OnPropertyChanged(nameof(GroupedArtistList));
        IsProgressRingActive = false;
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

    [RelayCommand]

    public void PlayButton(LocalArtistInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
        App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
    }

    [RelayCommand]

    public void PlayNextButton(LocalArtistInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToNextPlay(songList);
        }
    }

    [RelayCommand]

    public void AddToPlayQueueButton(LocalArtistInfo info)

    {
        var songList = App.GetService<MusicLibrary>().GetSongsByArtist(info);
        if (Data.PlayQueueManager.CurrentQueue.Count == 0)
        {
            Data.PlayQueueManager.SetNormalPlayQueue($"LocalSongs:Artist:{info.Name}", songList);
            App.GetService<MusicPlayer>().PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.PlayQueueManager.AddSongsToEnd(songList);
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

    public Visibility GetArtistGridViewVisibility(bool isActive)
    {
        return isActive ? Visibility.Collapsed : Visibility.Visible;
    }





    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}

