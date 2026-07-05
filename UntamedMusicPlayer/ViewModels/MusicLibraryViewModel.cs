using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using ZLinq;

using CommunityToolkit.Mvvm.Input;
namespace UntamedMusicPlayer.ViewModels;

public sealed partial class MusicLibraryViewModel
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
    public partial bool IsNoMusicControlVisible { get; set; } = false;

    [ObservableProperty]
    public partial bool IsHaveMusicControlVisible { get; set; } = false;

    public MusicLibraryViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        _ = InitializeLibraryAsync();
    }

    public void Receive(HaveMusicMessage message)
    {
        IsNoMusicControlVisible = !message.HasMusic;
        IsHaveMusicControlVisible = message.HasMusic;
    }

    private async Task InitializeLibraryAsync()
    {
        if (!App.GetService<MusicLibrary>().HasLoaded)
        {
            await App.GetService<MusicLibrary>().LoadLibraryAsync();
        }
        IsProgressRingActive = false;
        IsNoMusicControlVisible = App.GetService<MusicLibrary>().Songs.IsEmpty;
        IsHaveMusicControlVisible = !App.GetService<MusicLibrary>().Songs.IsEmpty;
    }

    [RelayCommand]

    public async Task PickMusicFolderButton()

    {
        var openPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
        };
        var folder = await openPicker.PickSingleFolderAsync();
        if (
            folder is not null
            && !App.GetService<MusicLibrary>().Folders.AsValueEnumerable().Contains(folder.Path)
        )
        {
            IsNoMusicControlVisible = false;
            IsHaveMusicControlVisible = false;
            IsProgressRingActive = true;
            App.GetService<MusicLibrary>().Folders.Add(folder.Path);
            await SettingsViewModel.SaveFoldersAsync();
            await App.GetService<MusicLibrary>().LoadLibraryAgainAsync();
            IsProgressRingActive = false;
        }
    }

    public async Task<int> LoadSelectionBarSelectedIndex()
    {
        return await _localSettingsService.ReadSettingAsync<int>(
            "HaveMusicSelectionBarSelectedIndex"
        );
    }

    public async Task SaveSelectionBarSelectedIndexAsync(int selectedIndex)
    {
        await _localSettingsService.SaveSettingAsync(
            "HaveMusicSelectionBarSelectedIndex",
            selectedIndex
        );
    }


    public void Dispose() => Messenger.Unregister<HaveMusicMessage>(this);
}

