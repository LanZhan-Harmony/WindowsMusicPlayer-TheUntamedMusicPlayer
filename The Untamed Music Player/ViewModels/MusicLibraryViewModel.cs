using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class MusicLibraryViewModel
    : ObservableRecipient,
        IRecipient<HaveMusicMessage>,
        IDisposable
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
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        InitializeLibraryAsync();
    }

    public void Receive(HaveMusicMessage message)
    {
        NoMusicControlVisibility = message.HasMusic ? Visibility.Collapsed : Visibility.Visible;
        HaveMusicControlVisibility = message.HasMusic ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void InitializeLibraryAsync()
    {
        if (!Data.HasMusicLibraryLoaded)
        {
            await Data.MusicLibrary.LoadLibraryAsync();
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
            FileTypeFilter = { "*" },
        };
        var hWnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        var folder = await openPicker.PickSingleFolderAsync();
        if (
            folder is not null
            && !Data.MusicLibrary.Folders.AsValueEnumerable().Any(f => f.Path == folder.Path)
        )
        {
            NoMusicControlVisibility = Visibility.Collapsed;
            HaveMusicControlVisibility = Visibility.Collapsed;
            IsProgressRingActive = true;
            Data.MusicLibrary.Folders.Add(folder);
            await SettingsViewModel.SaveFoldersAsync();
            await Data.MusicLibrary.LoadLibraryAgainAsync();
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

    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}
