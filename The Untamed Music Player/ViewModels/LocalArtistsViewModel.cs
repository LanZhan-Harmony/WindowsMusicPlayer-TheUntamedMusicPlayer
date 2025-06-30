using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    private List<ArtistInfo> _artistList = [.. Data.MusicLibrary.Artists.Values];

    public List<string> SortBy { get; set; } =
        [.. "LocalArtists_SortBy".GetLocalized().Split(", ")];

    public ObservableCollection<GroupInfoList> GroupedArtistList { get; set; } = [];

    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial byte SortMode { get; set; } = 0;

    partial void OnSortModeChanged(byte value)
    {
        SaveSortModeAsync();
    }

    public LocalArtistsViewModel()
    {
        LoadModeAndArtistList();
        Data.MusicLibrary.PropertyChanged += MusicLibrary_PropertyChanged;
    }

    public async void LoadModeAndArtistList()
    {
        await LoadSortModeAsync();
        await SortArtists();
        OnPropertyChanged(nameof(GroupedArtistList));
        IsProgressRingActive = false;
    }

    private void MusicLibrary_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "LibraryReloaded")
        {
            _artistList = [.. Data.MusicLibrary.Artists.Values];
            LoadModeAndArtistList();
        }
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

    public void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ArtistInfo artistInfo })
        {
            var songList = Data.MusicLibrary.GetSongsByArtist(artistInfo);
            Data.MusicPlayer.SetPlayList(
                $"LocalSongs:Artist:{artistInfo.Name}",
                songList,
                0,
                SortMode
            );
            Data.MusicPlayer.PlaySongByInfo(songList[0]);
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
                    m.Name == "MusicInfo_UnknownArtist".GetLocalized()
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
                    m.Name == "MusicInfo_UnknownArtist".GetLocalized()
                        ? "..."
                        : TitleComparer.GetGroupKey(m.Name[0])
                )
                .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = [.. sortedGroups];
        });
    }

    public async Task LoadSortModeAsync()
    {
        SortMode = await _localSettingsService.ReadSettingAsync<byte>("ArtistSortMode");
    }

    public async void SaveSortModeAsync()
    {
        await _localSettingsService.SaveSettingAsync("ArtistSortMode", SortMode);
    }

    public string GetSortByStr(byte SortMode)
    {
        return SortBy[SortMode];
    }

    public double GetArtistGridViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }
}
