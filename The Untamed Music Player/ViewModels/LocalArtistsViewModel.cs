using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public partial class LocalArtistsViewModel : ObservableRecipient
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

    public async void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentsortmode = SortMode;
        if (sender is ListView listView && listView.SelectedIndex is int selectedIndex)
        {
            SortMode = (byte)selectedIndex;
            if (SortMode != currentsortmode)
            {
                IsProgressRingActive = true;
                await SortArtists();
                OnPropertyChanged(nameof(GroupedArtistList));
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
        var tempList = Data.MusicLibrary.GetSongsByArtist(info);
        var songList = tempList.ToList();
        Data.MusicPlayer.SetPlayQueue($"LocalSongs:Artist:{info.Name}", songList, 0, SortMode);
        Data.MusicPlayer.PlaySongByInfo(songList[0]);
    }

    public void PlayNextButton_Click(LocalArtistInfo info)
    {
        var tempList = Data.MusicLibrary.GetSongsByArtist(info);
        var songList = tempList.ToList();
        if (Data.MusicPlayer.PlayQueue.Count == 0)
        {
            Data.MusicPlayer.SetPlayQueue($"LocalSongs:Artist:{info.Name}", songList, 0, SortMode);
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
        }
        else
        {
            Data.MusicPlayer.AddSongsToNextPlay(songList);
        }
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
}
