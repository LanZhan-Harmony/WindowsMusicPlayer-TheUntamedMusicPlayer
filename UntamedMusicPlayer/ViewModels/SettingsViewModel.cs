using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class SettingsViewModel
    : ObservableRecipient,
        IRecipient<HavePlaylistMessage>,
        IRecipient<MusicFoldersChangedMessage>,
        IDisposable
{
    private readonly IThemeSelectorService _themeSelectorService =
        App.GetService<IThemeSelectorService>();
    private readonly IMaterialSelectorService _materialSelectorService =
        App.GetService<IMaterialSelectorService>();
    private readonly IDynamicBackgroundService _dynamicBackgroundService =
        App.GetService<IDynamicBackgroundService>();
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly MusicPlayer _musicPlayer;

    /// <summary>
    /// 是否显示文件夹为空信息
    /// </summary>
    [ObservableProperty]
    public partial bool IsEmptyFolderMessageVisible { get; set; } = false;

    /// <summary>
    /// 歌曲下载位置
    /// </summary>
    [ObservableProperty]
    public partial string SongDownloadLocation { get; set; } = "";

    partial void OnSongDownloadLocationChanged(string value)
    {
        _ = SaveSongDownloadLocationAsync(value);
    }

    [ObservableProperty]
    public partial bool IsExportPlaylistsButtonEnabled { get; set; } = false;

    /// <summary>
    /// 是否为独占模式
    /// </summary>
    [ObservableProperty]
    public partial bool IsExclusiveMode { get; set; } = Settings.IsExclusiveMode;

    partial void OnIsExclusiveModeChanged(bool value)
    {
        Settings.IsExclusiveMode = value;
        _ = _musicPlayer.SetExclusiveModeAsync(value);
    }

    /// <summary>
    /// 是否为如果当前位于音乐库歌曲页面且使用文件夹排序方式，点击歌曲仅会将其所在文件夹内的歌曲加入播放队列
    /// </summary>
    [ObservableProperty]
    public partial bool IsOnlyAddSpecificFolder { get; set; } = Settings.IsOnlyAddSpecificFolder;

    partial void OnIsOnlyAddSpecificFolderChanged(bool value)
    {
        Settings.IsOnlyAddSpecificFolder = value;
    }

    /// <summary>
    /// 字体列表
    /// </summary>
    public List<FontFamilyInfo> FontFamilies { get; set; } = FontHelper.GetSystemFontFamilies();

    public double[] LyricPageCurrentFontSizes { get; set; } =
    [30, 35, 40, 45, 50, 55, 60, 65, 70, 75];
    public double[] LyricPageNotCurrentFontSizes { get; set; } =
    [10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65];

    public List<FontWeightInfo> FontWeights { get; set; } = FontHelper.GetFontWeights();

    /// <summary>
    /// 选中的字体
    /// </summary>
    [ObservableProperty]
    public partial FontFamily SelectedFontFamily { get; set; } = Settings.FontFamily;

    partial void OnSelectedFontFamilyChanged(FontFamily value)
    {
        Settings.FontFamily = value;
    }

    /// <summary>
    /// 选中的高亮字号
    /// </summary>
    [ObservableProperty]
    public partial double LyricPageCurrentFontSize { get; set; } =
        Settings.LyricPageCurrentFontSize;

    partial void OnLyricPageCurrentFontSizeChanged(double value)
    {
        Settings.LyricPageCurrentFontSize = value;
        Messenger.Send(new FontSizeChangeMessage());
    }

    /// <summary>
    /// 选中的非高亮字号
    /// </summary>
    [ObservableProperty]
    public partial double LyricPageNotCurrentFontSize { get; set; } =
        Settings.LyricPageNotCurrentFontSize;

    partial void OnLyricPageNotCurrentFontSizeChanged(double value)
    {
        Settings.LyricPageNotCurrentFontSize = value;
        Messenger.Send(new FontSizeChangeMessage());
    }

    /// <summary>
    /// 选中的字重
    /// </summary>
    [ObservableProperty]
    public partial FontWeight LyricPageFontWeight { get; set; } = Settings.LyricPageFontWeight;

    partial void OnLyricPageFontWeightChanged(FontWeight value)
    {
        Settings.LyricPageFontWeight = value;
    }

    [ObservableProperty]
    public partial int GlobalLyricOffset { get; set; } = Settings.GlobalLyricOffset;

    partial void OnGlobalLyricOffsetChanged(int value)
    {
        Settings.GlobalLyricOffset = value;
        Messenger.Send(new LyricOffsetChangeMessage(value));
    }

    /// <summary>
    /// 深浅色主题
    /// </summary>
    [ObservableProperty]
    public partial ElementTheme ElementTheme { get; set; } = Settings.Theme;

    [RelayCommand]
    public void SwitchTheme(ElementTheme theme)
    {
        if (ElementTheme != theme)
        {
            ElementTheme = theme;
            _themeSelectorService.SetThemeAsync(theme);
        }
    }

    /// <summary>
    /// 窗口材质列表
    /// </summary>
    public List<string> Materials { get; set; } =
    [.. "Settings_Materials".GetLocalized().Split(", ")];

    /// <summary>
    /// 选中的材质
    /// </summary>
    [ObservableProperty]
    public partial byte SelectedMaterial { get; set; } = (byte)Settings.Material;

    /// <summary>
    /// 是否启用窗口失去焦点回退
    /// </summary>
    [ObservableProperty]
    public partial bool IsFallBack { get; set; } = Settings.IsFallBack;

    partial void OnIsFallBackChanged(bool value)
    {
        _materialSelectorService.IsFallBack = value;
    }

    /// <summary>
    /// 不透明度
    /// </summary>
    [ObservableProperty]
    public partial byte LuminosityOpacity { get; set; } = Settings.LuminosityOpacity;

    partial void OnLuminosityOpacityChanged(byte value)
    {
        _materialSelectorService.SetLuminosityOpacity(value, false);
    }

    /// <summary>
    /// 背景颜色
    /// </summary>
    [ObservableProperty]
    public partial Color TintColor { get; set; } = Settings.TintColor;

    partial void OnTintColorChanged(Color value)
    {
        _materialSelectorService.SetTintColor(value, false);
    }

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    [ObservableProperty]
    public partial bool IsWindowBackgroundFollowsCover { get; set; } =
        Settings.IsWindowBackgroundFollowsCover;

    partial void OnIsWindowBackgroundFollowsCoverChanged(bool value)
    {
        _dynamicBackgroundService.IsEnabled = value;
    }

    /// <summary>
    /// 是否在全屏模式下自动隐藏播放控制栏
    /// </summary>
    [ObservableProperty]
    public partial bool IsAutoHidePlaybackControlBar { get; set; } =
        Settings.IsAutoHidePlaybackControlBar;

    partial void OnIsAutoHidePlaybackControlBarChanged(bool value)
    {
        Settings.IsAutoHidePlaybackControlBar = value;
    }

    /// <summary>
    /// 版本信息
    /// </summary>
    public string VersionDescription { get; set; } = GetVersionDescription();

    public SettingsViewModel(MusicPlayer musicPlayer)
        : base(StrongReferenceMessenger.Default)
    {
        _musicPlayer = musicPlayer;
        Messenger.Register<HavePlaylistMessage>(this);
        Messenger.Register<MusicFoldersChangedMessage>(this);

        UpdateEmptyFolderMessageState();
        _ = LoadSongDownloadLocationAsync();
        IsExportPlaylistsButtonEnabled = App.GetService<PlaylistLibrary>().Playlists.Count > 0;
    }

    public void Receive(HavePlaylistMessage message)
    {
        IsExportPlaylistsButtonEnabled = message.HasPlaylist;
    }

    public void Receive(MusicFoldersChangedMessage message)
    {
        UpdateEmptyFolderMessageState();
    }

    [RelayCommand]
    public async Task PickMusicFolderButton()
    {
        var openPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            CommitButtonText = "Settings_AddFolderToMusic".GetLocalized(),
        };
        var folder = await openPicker.PickSingleFolderAsync();
        if (
            folder is not null
            && !App.GetService<MusicLibrary>().Folders.AsValueEnumerable().Contains(folder.Path)
        )
        {
            App.GetService<MusicLibrary>().Folders.Add(folder.Path);
            UpdateEmptyFolderMessageState();
            await SaveFoldersAsync();
            await App.GetService<MusicLibrary>().LoadLibraryAgainAsync(); // 重新加载音乐库
        }
    }

    public async Task RemoveMusicFolderAsync(string folder)
    {
        App.GetService<MusicLibrary>().Folders.Remove(folder);
        UpdateEmptyFolderMessageState();
        await SaveFoldersAsync();
        await App.GetService<MusicLibrary>().LoadLibraryAgainAsync();
    }

    public void UpdateEmptyFolderMessageState()
    {
        IsEmptyFolderMessageVisible = App.GetService<MusicLibrary>().Folders.Count == 0;
    }

    [RelayCommand]
    public async Task RefreshButton()
    {
        await App.GetService<MusicLibrary>().LoadLibraryAgainAsync();
    }

    [RelayCommand]
    public void SongDownloadLocationButton()
    {
        Process.Start("explorer.exe", SongDownloadLocation);
    }

    [RelayCommand]
    public async Task ChangeSongDownloadLocationButton()
    {
        try
        {
            var openPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
            };
            var folder = await openPicker.PickSingleFolderAsync();
            if (folder is not null)
            {
                SongDownloadLocation = folder.Path;
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task ImportFromM3u8Button()
    {
        try
        {
            var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { ".m3u8", ".m3u" },
            };
            var files = await picker.PickMultipleFilesAsync();
            if (files.Count == 0)
            {
                return;
            }
            var infos = new List<PlaylistInfo>();
            foreach (var file in files)
            {
                var (name, cover, songs) = await M3u8Helper.GetNameAndSongsFromM3u8(file.Path);
                var info = new PlaylistInfo(name, cover);
                await info.AddSongs(songs);
                infos.Add(info);
            }
            App.GetService<PlaylistLibrary>().NewPlaylists(infos);
            Messenger.Send(
                new LogMessage(
                    LogLevel.None,
                    infos.Count == 1
                        ? "PlaylistInfo_ImportPlaylist".GetLocalizedWithReplace(
                            "{num}",
                            $"{infos.Count}"
                        )
                        : "PlaylistInfo_ImportPlaylists".GetLocalizedWithReplace(
                            "{num}",
                            $"{infos.Count}"
                        )
                )
            );
        }
        catch { }
    }

    [RelayCommand]
    public async Task ImportFromBinButton()
    {
        try
        {
            var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                FileTypeFilter = { ".bin" },
            };
            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                var playlists = await FileManager.LoadPlaylistDataFromBinAsync(file.Path);
                App.GetService<PlaylistLibrary>().NewPlaylists(playlists);
                Messenger.Send(
                    new LogMessage(
                        LogLevel.None,
                        playlists.Count == 1
                            ? "PlaylistInfo_ImportPlaylist".GetLocalizedWithReplace(
                                "{num}",
                                $"{playlists.Count}"
                            )
                            : "PlaylistInfo_ImportPlaylists".GetLocalizedWithReplace(
                                "{num}",
                                $"{playlists.Count}"
                            )
                    )
                );
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task ExportToM3u8Button()
    {
        try
        {
            var folderPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
            };
            var folder = await folderPicker.PickSingleFolderAsync();
            var count = App.GetService<PlaylistLibrary>().Playlists.Count;
            if (folder is not null && count != 0)
            {
                await M3u8Helper.ExportPlaylistsToM3u8Async(folder.Path);
                Messenger.Send(
                    new LogMessage(
                        LogLevel.None,
                        count == 1
                            ? "PlaylistInfo_ExportPlaylist".GetLocalizedWithReplace(
                                "{num}",
                                $"{count}"
                            )
                            : "PlaylistInfo_ExportPlaylists".GetLocalizedWithReplace(
                                "{num}",
                                $"{count}"
                            )
                    )
                );
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task ExportToBinButton()
    {
        try
        {
            var prepareBinTask = FileManager.SavePlaylistDataAsync(
                App.GetService<PlaylistLibrary>().Playlists
            );
            var savePicker = new FileSavePicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = "Settings_Playlist".GetLocalized(),
                FileTypeChoices = { { "Settings_PlaylistFile".GetLocalized(), [".bin"] } },
            };
            var file = await savePicker.PickSaveFileAsync();
            var count = App.GetService<PlaylistLibrary>().Playlists.Count;
            if (file is not null && count != 0)
            {
                await prepareBinTask;
                var binPath = Path.Combine(
                    ApplicationData.Current.LocalFolder.Path,
                    "PlaylistData",
                    "Playlists.bin"
                );
                var sourceFile = await StorageFile.GetFileFromPathAsync(binPath);
                var destFile = await StorageFile.GetFileFromPathAsync(file.Path);
                await sourceFile.CopyAndReplaceAsync(destFile);
                Messenger.Send(
                    new LogMessage(
                        LogLevel.None,
                        count == 1
                            ? "PlaylistInfo_ExportPlaylist".GetLocalizedWithReplace(
                                "{num}",
                                $"{count}"
                            )
                            : "PlaylistInfo_ExportPlaylists".GetLocalizedWithReplace(
                                "{num}",
                                $"{count}"
                            )
                    )
                );
            }
        }
        catch { }
    }

    public async Task UpdateSelectedMaterialAsync()
    {
        var (opacity, color) = await _materialSelectorService.SetMaterial(
            (MaterialType)SelectedMaterial,
            false,
            false
        );
        LuminosityOpacity = opacity;
        TintColor = color;
    }

    [RelayCommand]
    public async Task ResetMaterialButton()
    {
        IsFallBack = true;
        SelectedMaterial = (byte)MaterialType.DesktopAcrylic;
        var (opacity, color) = await _materialSelectorService.SetMaterial(
            (MaterialType)SelectedMaterial,
            false,
            true
        );
        LuminosityOpacity = opacity;
        TintColor = color;
        OnPropertyChanged(nameof(SelectedMaterial));
    }

    public void SelectFontFamily(FontFamilyInfo selectedFont)
    {
        SelectedFontFamily = new FontFamily(selectedFont.Name);
    }

    public void SelectLyricPageCurrentFontSize(double fontSize)
    {
        LyricPageCurrentFontSize = fontSize;
    }

    public void SelectLyricPageNotCurrentFontSize(double fontSize)
    {
        LyricPageNotCurrentFontSize = fontSize;
    }

    public void SelectFontWeight(FontWeightInfo selectedWeight)
    {
        LyricPageFontWeight = selectedWeight.FontWeight;
    }

    public bool TrySubmitLyricPageCurrentFontSize(string text)
    {
        if (double.TryParse(text, out var fontSize))
        {
            LyricPageCurrentFontSize = Math.Clamp(fontSize, 20, 100);
            return true;
        }

        return false;
    }

    public bool TrySubmitLyricPageNotCurrentFontSize(string text)
    {
        if (double.TryParse(text, out var fontSize))
        {
            LyricPageNotCurrentFontSize = Math.Clamp(fontSize, 5, 100);
            return true;
        }

        return false;
    }

    [RelayCommand]
    public void OpenLoggingFolderButton()
    {
        var logFolder = LoggingService.GetLogFolderPath();
        Directory.CreateDirectory(logFolder);
        Process.Start("explorer.exe", logFolder);
    }

    [RelayCommand]
    public async Task ResetSoftwareButton()
    {
        try
        {
            await ApplicationData.Current.ClearAsync();
        }
        catch { }
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("--reset-completed");
    }

    private static string GetVersionDescription()
    {
        Version version;
        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            version = new(
                packageVersion.Major,
                packageVersion.Minor,
                packageVersion.Build,
                packageVersion.Revision
            );
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }
        return $"{"Settings_Version".GetLocalized()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private async Task LoadSongDownloadLocationAsync()
    {
        var location = await _localSettingsService.ReadSettingAsync<string>("SongDownloadLocation");
        if (string.IsNullOrWhiteSpace(location))
        {
            var folder = (await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music))
                .Folders.AsValueEnumerable()
                .FirstOrDefault();
            location = folder?.Path;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                Directory.CreateDirectory(location);
            }
        }
        SongDownloadLocation = location;
    }

    public static async Task SaveFoldersAsync()
    {
        var folderPaths = App.GetService<MusicLibrary>().Folders?.AsValueEnumerable().ToList();
        await ApplicationData.Current.LocalFolder.SaveAsync("MusicFolders", folderPaths); //	调用 SettingsStorageExtensions 类中的扩展方法 SaveAsync，将 folderPaths 列表保存到名为 "MusicFolders" 的文件中。
    }

    private async Task SaveSongDownloadLocationAsync(string songDownloadLocation)
    {
        await _localSettingsService.SaveSettingAsync("SongDownloadLocation", songDownloadLocation);
    }

    public void Dispose()
    {
        Messenger.Unregister<HavePlaylistMessage>(this);
        Messenger.Unregister<MusicFoldersChangedMessage>(this);
    }
}
