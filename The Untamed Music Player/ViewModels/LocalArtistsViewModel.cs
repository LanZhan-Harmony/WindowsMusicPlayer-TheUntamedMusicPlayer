using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.ViewModels;

public class LocalArtistsViewModel : INotifyPropertyChanged
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool _isProgressRingActive = true;
    public bool IsProgressRingActive
    {
        get => _isProgressRingActive;
        set
        {
            _isProgressRingActive = value;
            OnPropertyChanged(nameof(IsProgressRingActive));
        }
    }

    private List<string> _sortBy = [.. "LocalArtists_SortBy".GetLocalized().Split(", ")];
    public List<string> SortBy
    {
        get => _sortBy;
        set => _sortBy = value;
    }

    private byte _sortMode;
    public byte SortMode
    {
        get => _sortMode;
        set
        {
            _sortMode = value;
            OnPropertyChanged(nameof(SortMode));
            SaveSortModeAsync();
        }
    }

    private List<ArtistInfo> _artistList = [.. Data.MusicLibrary.Artists.Values];

    public List<ArtistInfo> ArtistList
    {
        get => _artistList;
        set => _artistList = value;
    }

    private ObservableCollection<GroupInfoList> _groupedArtistList = [];


    public ObservableCollection<GroupInfoList> GroupedArtistList
    {
        get => _groupedArtistList;
        set => _groupedArtistList = value;
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
        if ((e.PropertyName == "LibraryReloaded"))
        {
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

    public string GetSortByStr(byte SortMode)
    {
        return SortBy[SortMode];
    }

    public double GetArtistGridViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }

    public async Task SortArtists()
    {
        var sortTask = SortMode switch
        {
            0 => SortArtistsByTitleAscending(),
            1 => SortArtistsByTitleDescending(),
            _ => SortArtistsByTitleAscending()
        };

        await sortTask;
    }

    public async Task SortArtistsByTitleAscending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = ArtistList
               .OrderBy(m => m.Name, new ArtistTitleComparer())
               .GroupBy(m => m.Name == "MusicInfo_UnknownArtist".GetLocalized() ? "..." : TitleComparer.GetGroupKey(m.Name[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = [.. sortedGroups];
        });
    }

    public async Task SortArtistsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = ArtistList
               .OrderByDescending(m => m.Name, new ArtistTitleComparer())
               .GroupBy(m => m.Name == "MusicInfo_UnknownArtist".GetLocalized() ? "..." : TitleComparer.GetGroupKey(m.Name[0]))
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
}
