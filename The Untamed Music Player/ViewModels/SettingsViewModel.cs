using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;
using ZLinq;

namespace The_Untamed_Music_Player.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;

    /// <summary>
    /// 是否显示文件夹为空信息
    /// </summary>
    public Visibility EmptyFolderMessageVisibility =>
        Data.MusicLibrary.Folders?.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// 歌曲下载位置
    /// </summary>
    [ObservableProperty]
    public partial string SongDownloadLocation { get; set; } = "";

    partial void OnSongDownloadLocationChanged(string value)
    {
        SaveSongDownloadLocationAsync(value);
    }

    /// <summary>
    /// 是否启用窗口失去焦点回退
    /// </summary>
    [ObservableProperty]
    public partial bool IsFallBack { get; set; } = Data.MainViewModel!.IsFallBack;

    partial void OnIsFallBackChanged(bool value)
    {
        Data.MainViewModel!.IsFallBack = value;
        SaveIsFallBackAsync(value);
    }

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    [ObservableProperty]
    public partial bool IsWindowBackgroundFollowsCover { get; set; } =
        Data.IsWindowBackgroundFollowsCover;

    partial void OnIsWindowBackgroundFollowsCoverChanged(bool value)
    {
        var dynamicBackgroundService = App.GetService<IDynamicBackgroundService>();
        dynamicBackgroundService.IsEnabled = value;
        Data.IsWindowBackgroundFollowsCover = value;
        SaveLyricBackgroundVisibilityAsync(value);
    }

    /// <summary>
    /// 是否为独占模式
    /// </summary>
    [ObservableProperty]
    public partial bool IsExclusiveMode { get; set; }

    partial void OnIsExclusiveModeChanged(bool value)
    {
        SaveExclusiveModeAsync(value);
    }

    /// <summary>
    /// 是否为如果当前位于音乐库歌曲页面且使用文件夹排序方式，点击歌曲仅会将其所在文件夹内的歌曲加入播放队列
    /// </summary>
    [ObservableProperty]
    public partial bool IsOnlyAddSpecificFolder { get; set; }

    partial void OnIsOnlyAddSpecificFolderChanged(bool value)
    {
        SaveOnlyAddSpecificFolderAsync(value);
    }

    /// <summary>
    /// 字体列表
    /// </summary>
    public List<FontInfo> FontFamilies { get; set; } = [];

    public List<double> FontSizes { get; set; } = [30, 35, 40, 45, 50, 55, 60, 65, 70, 75];

    /// <summary>
    /// 窗口材质列表
    /// </summary>
    public List<string> Materials { get; set; } =
        [.. "Settings_Materials".GetLocalized().Split(", ")];

    /// <summary>
    /// 深浅色主题
    /// </summary>
    [ObservableProperty]
    public partial ElementTheme ElementTheme { get; set; }

    /// <summary>
    /// 版本信息
    /// </summary>
    [ObservableProperty]
    public partial string VersionDescription { get; set; }

    /// <summary>
    /// 选中的字体
    /// </summary>
    [ObservableProperty]
    public partial FontFamily SelectedFontFamily { get; set; } = Data.SelectedFontFamily;

    partial void OnSelectedFontFamilyChanged(FontFamily value)
    {
        Data.SelectedFontFamily = value;
        SaveSelectedFontFamilyAsync(value.Source);
    }

    /// <summary>
    /// 选中的高亮字号
    /// </summary>
    [ObservableProperty]
    public partial double SelectedCurrentFontSize { get; set; } = Data.SelectedCurrentFontSize;

    partial void OnSelectedCurrentFontSizeChanged(double value)
    {
        Data.SelectedCurrentFontSize = value;
        SelectedNotCurrentFontSize = value * 0.4;
        SaveSelectedFontSizeAsync(value);
    }

    /// <summary>
    /// 选中的非高亮字号
    /// </summary>
    [ObservableProperty]
    public partial double SelectedNotCurrentFontSize { get; set; } =
        Data.SelectedNotCurrentFontSize;

    partial void OnSelectedNotCurrentFontSizeChanged(double value)
    {
        Data.SelectedNotCurrentFontSize = value;
        Messenger.Send(new FontSizeChangeMessage());
        SaveSelectedFontSizeAsync(value);
    }

    /// <summary>
    /// 选中的材质
    /// </summary>
    [ObservableProperty]
    public partial byte SelectedMaterial { get; set; } = Data.MainViewModel!.SelectedMaterial;

    partial void OnSelectedMaterialChanged(byte value)
    {
        SaveSelectedMaterialAsync(value);
    }

    /// <summary>
    /// 透明度
    /// </summary>
    [ObservableProperty]
    public partial byte LuminosityOpacity { get; set; } = Data.MainViewModel!.LuminosityOpacity;

    partial void OnLuminosityOpacityChanged(byte value)
    {
        Data.MainViewModel!.LuminosityOpacity = value;
        SaveLuminosityOpacityAsync(value);
    }

    /// <summary>
    /// 背景颜色
    /// </summary>
    [ObservableProperty]
    public partial Color TintColor { get; set; } = Data.MainViewModel!.TintColor;

    partial void OnTintColorChanged(Color value)
    {
        Data.MainViewModel!.TintColor = value;
        SaveTintColorAsync(value);
    }

    [RelayCommand]
    public async Task SwitchThemeAsync(ElementTheme theme)
    {
        if (ElementTheme != theme)
        {
            ElementTheme = theme;
            await _themeSelectorService.SetThemeAsync(theme);
        }
    }

    public SettingsViewModel()
        : base(StrongReferenceMessenger.Default)
    {
        _themeSelectorService = App.GetService<IThemeSelectorService>();
        _localSettingsService = App.GetService<ILocalSettingsService>();
        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        LoadSongDownloadLocationAsync();
        LoadFonts();
        Data.SettingsViewModel = this;
    }

    /// <summary>
    /// 通知 EmptyFolderMessageVisibility 属性发生了变化（供外部调用）
    /// </summary>
    public void NotifyEmptyFolderMessageVisibilityChanged()
    {
        OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
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
        if (
            folder is not null
            && !Data.MusicLibrary.Folders.AsValueEnumerable().Any(f => f.Path == folder.Path)
        )
        {
            Data.MusicLibrary.Folders.Add(folder);
            OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
            await SaveFoldersAsync();
            await Data.MusicLibrary.LoadLibraryAgainAsync(); // 重新加载音乐库
        }
        (sender as Button)!.IsEnabled = true;
    }

    public async void RemoveMusicFolder(StorageFolder folder)
    {
        Data.MusicLibrary.Folders?.Remove(folder);
        OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
        await SaveFoldersAsync();
        await Data.MusicLibrary.LoadLibraryAgainAsync();
    }

    public async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton!.IsEnabled = false;
        await Data.MusicLibrary.LoadLibraryAgainAsync();
        senderButton!.IsEnabled = true;
    }

    public void SongDownloadLocationButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("explorer.exe", SongDownloadLocation);
    }

    public async void ChangeSongDownloadLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton!.IsEnabled = false;
        var openPicker = new FolderPicker();
        var window = App.MainWindow;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        openPicker.FileTypeFilter.Add("*");
        var folder = await openPicker.PickSingleFolderAsync();
        if (folder is not null)
        {
            SongDownloadLocation = folder.Path;
        }
        senderButton!.IsEnabled = true;
    }

    public void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedMaterial != Data.MainViewModel!.SelectedMaterial)
        {
            Data.MainViewModel.SelectedMaterial = SelectedMaterial;
            Data.MainViewModel.ChangeMaterial(SelectedMaterial);
            LuminosityOpacity = Data.MainViewModel.LuminosityOpacity;
            TintColor = Data.MainViewModel.TintColor;
        }
    }

    public void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        IsFallBack = true;
        SelectedMaterial = 3;
        Data.MainViewModel!.ChangeMaterial(SelectedMaterial);
        LuminosityOpacity = Data.MainViewModel.LuminosityOpacity;
        TintColor = Data.MainViewModel.TintColor;
        OnPropertyChanged(nameof(SelectedMaterial));
    }

    public void LuminosityOpacitySlider_ValueChanged(
        object sender,
        RangeBaseValueChangedEventArgs e
    )
    {
        Data.MainViewModel!.ChangeLuminosityOpacity(LuminosityOpacity);
    }

    public void TintColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        Data.MainViewModel!.ChangeTintColor(TintColor);
    }

    public void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FontInfo selectedFont)
        {
            SelectedFontFamily = new FontFamily(selectedFont.Name);
        }
    }

    public void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

    public void MaterialComboBox_Loaded(object sender, RoutedEventArgs e)
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

    public void FontFamilyComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        var selectedFontName = SelectedFontFamily.Source;
        var index = FontFamilies.FindIndex(f => f.Name == selectedFontName);
        if (index >= 0)
        {
            (sender as ComboBox)!.SelectedIndex = index;
        }
    }

    public void FontSizeComboBox_Loaded(object sender, RoutedEventArgs e)
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

    public void OpenLoggingFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var logFolder = LoggingService.GetLogFolderPath();
        Directory.CreateDirectory(logFolder);
        Process.Start("explorer.exe", logFolder);
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
        var folderPaths = Data
            .MusicLibrary.Folders?.AsValueEnumerable()
            .Select(f => f.Path)
            .ToList();
        await ApplicationData.Current.LocalFolder.SaveAsync("MusicFolders", folderPaths); //	ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。SaveAsync("MusicFolders", folderPaths)：调用 SettingsStorageExtensions 类中的扩展方法 SaveAsync，将 folderPaths 列表保存到名为 "MusicFolders" 的文件中。
    }

    private async void SaveSongDownloadLocationAsync(string songDownloadLocation)
    {
        await _localSettingsService.SaveSettingAsync("SongDownloadLocation", songDownloadLocation);
    }

    private async void SaveSelectedMaterialAsync(byte material)
    {
        await _localSettingsService.SaveSettingAsync("SelectedMaterial", material);
    }

    private async void SaveIsFallBackAsync(bool isFallBack)
    {
        await _localSettingsService.SaveSettingAsync("IsFallBack", isFallBack);
    }

    private async void SaveLuminosityOpacityAsync(byte luminosityOpacity)
    {
        await _localSettingsService.SaveSettingAsync("LuminosityOpacity", luminosityOpacity);
    }

    private async void SaveTintColorAsync(Color tintColor)
    {
        await _localSettingsService.SaveSettingAsync("TintColor", tintColor);
    }

    private async void SaveLyricBackgroundVisibilityAsync(bool isWindowBackgroundFollowsCover)
    {
        await _localSettingsService.SaveSettingAsync(
            "IsWindowBackgroundFollowsCover",
            isWindowBackgroundFollowsCover
        );
    }

    private async void SaveExclusiveModeAsync(bool isExclusiveMode)
    {
        await _localSettingsService.SaveSettingAsync("IsExclusiveMode", isExclusiveMode);
    }

    private async void SaveOnlyAddSpecificFolderAsync(bool isOnlyAddSpecificFolder)
    {
        await _localSettingsService.SaveSettingAsync(
            "IsOnlyAddSpecificFolder",
            isOnlyAddSpecificFolder
        );
    }

    private async void SaveSelectedFontFamilyAsync(string fontName)
    {
        await _localSettingsService.SaveSettingAsync("SelectedFontFamily", fontName);
    }

    private async void SaveSelectedFontSizeAsync(double fontSize)
    {
        await _localSettingsService.SaveSettingAsync("SelectedFontSize", fontSize);
    }
}
