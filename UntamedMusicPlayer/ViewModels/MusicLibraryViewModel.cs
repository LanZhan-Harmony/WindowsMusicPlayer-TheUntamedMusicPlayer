using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    public partial Visibility NoMusicControlVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility HaveMusicControlVisibility { get; set; } = Visibility.Collapsed;

    public MusicLibraryViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        _ = InitializeLibraryAsync();
    }

    public void Receive(HaveMusicMessage message)
    {
        NoMusicControlVisibility = message.HasMusic ? Visibility.Collapsed : Visibility.Visible;
        HaveMusicControlVisibility = message.HasMusic ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task InitializeLibraryAsync()
    {
        if (!App.GetService<MusicLibrary>().HasLoaded)
        {
            await App.GetService<MusicLibrary>().LoadLibraryAsync();
        }
        IsProgressRingActive = false;
        NoMusicControlVisibility = App.GetService<MusicLibrary>().Songs.IsEmpty
            ? Visibility.Visible
            : Visibility.Collapsed;
        HaveMusicControlVisibility = App.GetService<MusicLibrary>().Songs.IsEmpty
            ? Visibility.Collapsed
            : Visibility.Visible;
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
            NoMusicControlVisibility = Visibility.Collapsed;
            HaveMusicControlVisibility = Visibility.Collapsed;
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

