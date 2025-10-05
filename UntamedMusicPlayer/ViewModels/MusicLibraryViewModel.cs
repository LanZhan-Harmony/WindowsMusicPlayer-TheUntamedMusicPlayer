using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

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
        if (!Data.MusicLibrary.HasLoaded)
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

    public async void PickMusicFolderButton_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
        var openPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        var folder = await openPicker.PickSingleFolderAsync();
        if (
            folder is not null
            && !Data.MusicLibrary.Folders.AsValueEnumerable().Contains(folder.Path)
        )
        {
            NoMusicControlVisibility = Visibility.Collapsed;
            HaveMusicControlVisibility = Visibility.Collapsed;
            IsProgressRingActive = true;
            Data.MusicLibrary.Folders.Add(folder.Path);
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
