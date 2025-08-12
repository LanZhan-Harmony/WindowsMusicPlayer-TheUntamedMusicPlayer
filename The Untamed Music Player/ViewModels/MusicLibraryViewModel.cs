using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace The_Untamed_Music_Player.ViewModels;

public partial class MusicLibraryViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    /// <summary>
    /// 是否显示加载进度环
    /// </summary>
    [ObservableProperty]
    public partial bool IsProgressRingActive { get; set; } = true;

    [ObservableProperty]
    public partial Visibility NoMusicControlVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility HaveMusicControlVisibility { get; set; } = Visibility.Collapsed;

    public MusicLibraryViewModel()
    {
        InitializeLibraryAsync();
    }

    private async void InitializeLibraryAsync()
    {
        if (!Data.HasMusicLibraryLoaded)
        {
            await Task.Run(Data.MusicLibrary.LoadLibraryAsync);
        }
        IsProgressRingActive = false;
        NoMusicControlVisibility = Data.MusicLibrary.Songs.IsEmpty
            ? Visibility.Visible
            : Visibility.Collapsed;
        HaveMusicControlVisibility = Data.MusicLibrary.Songs.IsEmpty
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public async void PickMusicFolderButton_Click(object sender, RoutedEventArgs e)
    {
        (sender as Button)!.IsEnabled = false;
        var openPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        openPicker.FileTypeFilter.Add("*");
        var window = App.MainWindow;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);

        var folder = await openPicker.PickSingleFolderAsync();
        if (folder is not null && !Data.MusicLibrary.Folders.Any(f => f.Path == folder.Path))
        {
            NoMusicControlVisibility = Visibility.Collapsed;
            HaveMusicControlVisibility = Visibility.Collapsed;
            IsProgressRingActive = true;
            Data.MusicLibrary.Folders.Add(folder);
            await SettingsViewModel.SaveFoldersAsync();
            await Task.Run(Data.MusicLibrary.LoadLibraryAgainAsync);
            IsProgressRingActive = false;
        }
        (sender as Button)!.IsEnabled = true;
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "HaveMusicSelectionBarSelectedIndex"
        );
    }

    public async void SaveSelectionBarSelectedIndex(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "HaveMusicSelectionBarSelectedIndex",
            selectedIndex
        );
    }
}
