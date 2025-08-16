using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.ViewModels;

public partial class PlayListsViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
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
    public partial bool IsProgressRingActive { get; set; } = true;

    public PlayListsViewModel()
    {
        LoadModeAndPlayList();
    }

    public async void LoadModeAndPlayList()
    {
        await LoadSortModeAsync();
    }

    public void SortByListView_Loaded(object sender, RoutedEventArgs e)
    {
        (sender as ListView)!.SelectedIndex = SortMode;
    }

    public void SortByListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var currentsortmode = SortMode;
        if (sender is ListView listView && listView.SelectedIndex is int selectedIndex)
        {
            SortMode = (byte)selectedIndex;
            if (SortMode != currentsortmode)
            {
                IsProgressRingActive = true;
                IsProgressRingActive = false;
            }
        }
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
}
