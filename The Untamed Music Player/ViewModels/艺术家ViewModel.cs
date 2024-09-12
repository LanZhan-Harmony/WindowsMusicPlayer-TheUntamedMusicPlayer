using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using TagLib;
using Microsoft.UI.Xaml.Input;

namespace The_Untamed_Music_Player.ViewModels;

public class 艺术家ViewModel : INotifyPropertyChanged
{
    private readonly ILocalSettingsService _localSettingsService;
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

    private List<string> _sortBy = [.. "艺术家_SortBy".GetLocalized().Split(", ")];
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

    public 艺术家ViewModel(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        LoadModeAndArtistList();
    }

    public async void LoadModeAndArtistList()
    {
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

    public string GetSortByStr(byte SortMode)
    {
        return SortBy[SortMode];
    }

    public double GetArtistGridViewOpacity(bool isActive)
    {
        return isActive ? 0 : 1;
    }

    public void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Visible;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Visible;
        }
        if (menuButton != null)
        {
            menuButton.Visibility = Visibility.Visible;
        }
    }

    public void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var checkBox = grid?.FindName("ItemCheckBox") as CheckBox;
        var playButton = grid?.FindName("PlayButton") as Button;
        var menuButton = grid?.FindName("MenuButton") as Button;
        if (checkBox != null)
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
        if (playButton != null)
        {
            playButton.Visibility = Visibility.Collapsed;
        }
        if (menuButton != null)
        {
            menuButton.Visibility = Visibility.Collapsed;
        }
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
               .OrderBy(m => m.Name, new TitleComparer())
               .GroupBy(m => TitleComparer.GetGroupKey(m.Name[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = new ObservableCollection<GroupInfoList>(sortedGroups);
        });
    }

    public async Task SortArtistsByTitleDescending()
    {
        await Task.Run(() =>
        {
            var sortedGroups = ArtistList
               .OrderByDescending(m => m.Name, new TitleComparer())
               .GroupBy(m => TitleComparer.GetGroupKey(m.Name[0]))
               .Select(g => new GroupInfoList(g) { Key = g.Key });

            GroupedArtistList = new ObservableCollection<GroupInfoList>(sortedGroups);
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
