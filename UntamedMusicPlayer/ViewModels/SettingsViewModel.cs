using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using ZLinq;

namespace UntamedMusicPlayer.ViewModels;

public sealed partial class SettingsViewModel
    : ObservableRecipient,
        IRecipient<HavePlaylistMessage>,
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

    /// <summary>
    /// 是否显示文件夹为空信息
    /// </summary>
    [ObservableProperty]
    public partial Visibility EmptyFolderMessageVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// 歌曲下载位置
    /// </summary>
    [ObservableProperty]
    public partial string SongDownloadLocation { get; set; } = "";

    partial void OnSongDownloadLocationChanged(string value)
    {
        SaveSongDownloadLocationAsync(value);
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
        Data.MusicPlayer.SetExclusiveMode(value);
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
    public List<FontInfo> FontFamilies { get; set; } = [];

    public double[] FontSizes { get; set; } = [30, 35, 40, 45, 50, 55, 60, 65, 70, 75];

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
    public partial double SelectedCurrentFontSize { get; set; } = Settings.LyricPageCurrentFontSize;

    partial void OnSelectedCurrentFontSizeChanged(double value)
    {
        Settings.LyricPageCurrentFontSize = value;
        SelectedNotCurrentFontSize = value * 0.4;
    }

    /// <summary>
    /// 选中的非高亮字号
    /// </summary>
    [ObservableProperty]
    public partial double SelectedNotCurrentFontSize { get; set; } =
        Settings.LyricPageNotCurrentFontSize;

    partial void OnSelectedNotCurrentFontSizeChanged(double value)
    {
        Messenger.Send(new FontSizeChangeMessage());
    }

    /// <summary>
    /// 深浅色主题
    /// </summary>
    [ObservableProperty]
    public partial ElementTheme ElementTheme { get; set; }

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
    public partial byte SelectedMaterial { get; set; }

    /// <summary>
    /// 是否启用窗口失去焦点回退
    /// </summary>
    [ObservableProperty]
    public partial bool IsFallBack { get; set; }

    partial void OnIsFallBackChanged(bool value)
    {
        _materialSelectorService.IsFallBack = value;
    }

    /// <summary>
    /// 不透明度
    /// </summary>
    [ObservableProperty]
    public partial byte LuminosityOpacity { get; set; }

    partial void OnLuminosityOpacityChanged(byte value)
    {
        _materialSelectorService.SetLuminosityOpacity(value, false);
    }

    /// <summary>
    /// 背景颜色
    /// </summary>
    [ObservableProperty]
    public partial Color TintColor { get; set; }

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
    /// 版本信息
    /// </summary>
    [ObservableProperty]
    public partial string VersionDescription { get; set; }

    public SettingsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        Messenger.Register(this);
        ElementTheme = Settings.Theme;
        IsFallBack = Settings.IsFallBack;
        SelectedMaterial = (byte)Settings.Material;
        LuminosityOpacity = Settings.LuminosityOpacity;
        TintColor = Settings.TintColor;
        VersionDescription = GetVersionDescription();

        EmptyFolderMessageVisibility =
            Data.MusicLibrary.Folders.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        LoadSongDownloadLocationAsync();
        LoadFonts();
        IsExportPlaylistsButtonEnabled = Data.PlaylistLibrary.Playlists.Count > 0;
        Data.SettingsViewModel = this;
    }

    public void Receive(HavePlaylistMessage message)
    {
        IsExportPlaylistsButtonEnabled = message.HasPlaylist;
    }

    public async void PickMusicFolderButton_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
        var openPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary,
            CommitButtonText = "Settings_AddFolderToMusic".GetLocalized(),
        };
        var folder = await openPicker.PickSingleFolderAsync();
        if (
            folder is not null
            && !Data.MusicLibrary.Folders.AsValueEnumerable().Contains(folder.Path)
        )
        {
            Data.MusicLibrary.Folders.Add(folder.Path);
            EmptyFolderMessageVisibility =
                Data.MusicLibrary.Folders.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            await SaveFoldersAsync();
            await Data.MusicLibrary.LoadLibraryAgainAsync(); // 重新加载音乐库
        }
        (sender as Button)!.IsEnabled = true;
    }

    public async void RemoveMusicFolder(string folder)
    {
        Data.MusicLibrary.Folders.Remove(folder);
        EmptyFolderMessageVisibility =
            Data.MusicLibrary.Folders.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        await SaveFoldersAsync();
        await Data.MusicLibrary.LoadLibraryAgainAsync();
    }

    public async void RefreshButton_Click(object sender, RoutedEventArgs _)
    {
        var senderButton = sender as Button;
        senderButton!.IsEnabled = false;
        await Data.MusicLibrary.LoadLibraryAgainAsync();
        senderButton!.IsEnabled = true;
    }

    public void SongDownloadLocationButton_Click(object _1, RoutedEventArgs _2)
    {
        Process.Start("explorer.exe", SongDownloadLocation);
    }

    public async void ChangeSongDownloadLocationButton_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
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
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    public async void ImportFromM3u8Button_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
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
            Data.PlaylistLibrary.NewPlaylists(infos);
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
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    public async void ImportFromBinButton_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
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
                foreach (var playlist in playlists)
                {
                    playlist.InitializeCover();
                    playlist.GetCover();
                }
                Data.PlaylistLibrary.NewPlaylists(playlists);
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
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    public async void ExportToM3u8Button_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            var folderPicker = new FolderPicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
            };
            var folder = await folderPicker.PickSingleFolderAsync();
            var count = Data.PlaylistLibrary.Playlists.Count;
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
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    public async void ExportToBinButton_Click(object sender, RoutedEventArgs _)
    {
        (sender as Button)!.IsEnabled = false;
        try
        {
            var prepareBinTask = FileManager.SavePlaylistDataAsync(Data.PlaylistLibrary.Playlists);
            var savePicker = new FileSavePicker(App.MainWindow!.AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = "Settings_Playlist".GetLocalized(),
                FileTypeChoices = { { "Settings_PlaylistFile".GetLocalized(), [".bin"] } },
            };
            var file = await savePicker.PickSaveFileAsync();
            var count = Data.PlaylistLibrary.Playlists.Count;
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
        finally
        {
            (sender as Button)!.IsEnabled = true;
        }
    }

    public async void MaterialComboBox_SelectionChanged(object _1, SelectionChangedEventArgs _2)
    {
        var (opacity, color) = await _materialSelectorService.SetMaterial(
            (MaterialType)SelectedMaterial,
            false,
            false
        );
        LuminosityOpacity = opacity;
        TintColor = color;
    }

    public async void ResetMaterialButton_Click(object _1, RoutedEventArgs _2)
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

    public void FontFamilyComboBox_SelectionChanged(object _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FontInfo selectedFont)
        {
            SelectedFontFamily = new FontFamily(selectedFont.Name);
        }
    }

    public void FontSizeComboBox_SelectionChanged(object _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is double fontSize)
        {
            SelectedCurrentFontSize = fontSize;
        }
    }

    public void ComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
    {
        if (double.TryParse(args.Text, out var fontSize))
        {
            SelectedCurrentFontSize = Math.Clamp(fontSize, 20, 100);
        }
        else
        {
            sender.Text = $"{SelectedCurrentFontSize}";
        }
    }

    public void MaterialComboBox_Loaded(object sender, RoutedEventArgs _)
    {
        (sender as ComboBox)!.SelectedIndex = SelectedMaterial;
    }

    public void LoadFonts()
    {
        var language = new string[] { CultureInfo.CurrentUICulture.Name.ToLowerInvariant() };
        var names = CanvasTextFormat.GetSystemFontFamilies();
        var displayNames = CanvasTextFormat.GetSystemFontFamilies(language);
        var list = new List<FontInfo>();
        for (var i = 0; i < names.Length; i++)
        {
            list.Add(
                new FontInfo
                {
                    Name = names[i],
                    DisplayName = displayNames[i],
                    FontFamily = new FontFamily(names[i]),
                }
            );
        }
        FontFamilies = [.. list.AsValueEnumerable().OrderBy(f => f.Name)];
    }

    public void FontFamilyComboBox_Loaded(object sender, RoutedEventArgs _)
    {
        var selectedFontName = SelectedFontFamily.Source;
        var index = FontFamilies.FindIndex(f => f.Name == selectedFontName);
        if (index >= 0)
        {
            (sender as ComboBox)!.SelectedIndex = index;
        }
    }

    public void FontSizeComboBox_Loaded(object sender, RoutedEventArgs _)
    {
        var selectedItem = FontSizes.FirstOrDefault(f => f == SelectedCurrentFontSize);
        if (selectedItem != 0.0)
        {
            (sender as ComboBox)!.SelectedItem = selectedItem;
        }
        else
        {
            (sender as ComboBox)!.Text = $"{SelectedCurrentFontSize}";
        }
    }

    public void OpenLoggingFolderButton_Click(object _1, RoutedEventArgs _2)
    {
        var logFolder = LoggingService.GetLogFolderPath();
        Directory.CreateDirectory(logFolder);
        Process.Start("explorer.exe", logFolder);
    }

    public async Task ResetSoftwareButton_Click()
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

    private async void LoadSongDownloadLocationAsync()
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
                location = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Music"
                );
                Directory.CreateDirectory(location);
            }
        }
        SongDownloadLocation = location;
    }

    public static async Task SaveFoldersAsync()
    {
        var folderPaths = Data.MusicLibrary.Folders?.AsValueEnumerable().ToList();
        await ApplicationData.Current.LocalFolder.SaveAsync("MusicFolders", folderPaths); //	调用 SettingsStorageExtensions 类中的扩展方法 SaveAsync，将 folderPaths 列表保存到名为 "MusicFolders" 的文件中。
    }

    private async void SaveSongDownloadLocationAsync(string songDownloadLocation)
    {
        await _localSettingsService.SaveSettingAsync("SongDownloadLocation", songDownloadLocation);
    }

    public void Dispose()
    {
        Messenger.Unregister<HavePlaylistMessage>(this);
    }
}
