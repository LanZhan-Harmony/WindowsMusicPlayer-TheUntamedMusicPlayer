using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

namespace The_Untamed_Music_Player.ViewModels;
public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;

    public ICommand SwitchThemeCommand { get; }

    /// <summary>
    /// 是否显示文件夹为空信息
    /// </summary>
    public Visibility EmptyFolderMessageVisibility => Data.MusicLibrary.Folders?.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

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
    public bool IsLyricBackgroundVisible
    {
        get;
        set
        {
            field = value;
            Data.IsLyricBackgroundVisible = value;
            SaveLyricBackgroundVisibilityAsync(value);
        }
    }

    /// <summary>
    /// 字体列表
    /// </summary>
    public List<string> Fonts { get; set; } = [];

    /// <summary>
    /// 窗口材质列表
    /// </summary>
    public List<string> Materials { get; set; } = [.. "Settings_Materials".GetLocalized().Split(", ")];

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
    public partial FontFamily SelectedFont { get; set; } = Data.SelectedFont;
    partial void OnSelectedFontChanged(FontFamily value)
    {
        Data.SelectedFont = value;
        SaveSelectedFontAsync(value.Source);
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

    public SettingsViewModel()
    {
        _themeSelectorService = App.GetService<IThemeSelectorService>();
        _localSettingsService = App.GetService<ILocalSettingsService>();
        ElementTheme = _themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        LoadSongDownloadLocationAsync();
        LoadFonts();
        Data.SettingsViewModel = this;
    }

    public async void PickMusicFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton!.IsEnabled = false;
        var openPicker = new FolderPicker();
        var window = App.MainWindow;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        openPicker.FileTypeFilter.Add("*");

        var folder = await openPicker.PickSingleFolderAsync();
        if (folder is not null)
        {
            if (!Data.MusicLibrary.Folders.Any(f => f.Path == folder.Path))
            {
                Data.MusicLibrary.Folders.Add(folder);
                OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
                await SaveFoldersAsync();
                await Task.Run(Data.MusicLibrary.LoadLibraryAgainAsync); // 重新加载音乐库
            }
        }
        senderButton!.IsEnabled = true;
    }

    public async void RemoveMusicFolder(StorageFolder folder)
    {
        Data.MusicLibrary.Folders?.Remove(folder);
        OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
        await SaveFoldersAsync();
        await Task.Run(Data.MusicLibrary.LoadLibraryAgainAsync);
    }

    public async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var senderButton = sender as Button;
        senderButton!.IsEnabled = false;
        await Task.Run(Data.MusicLibrary.LoadLibraryAgainAsync);
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
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
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

    public void LuminosityOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        Data.MainViewModel!.ChangeLuminosityOpacity(LuminosityOpacity);
    }

    public void TintColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        Data.MainViewModel!.ChangeTintColor(TintColor);
    }

    public void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedFont)
        {
            SelectedFont = new FontFamily(selectedFont);
        }
    }

    public void MaterialComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.SelectedIndex = SelectedMaterial;
        }
    }

    public void LoadFonts()
    {
        var fontFamilies = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
        Fonts = [.. fontFamilies.OrderBy(f => f)];
    }

    public void FontComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            var selectedFontName = SelectedFont.Source;
            var index = Fonts.IndexOf(selectedFontName);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }
        }
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
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
            var folder = (await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music)).Folders.FirstOrDefault();
            location = folder?.Path;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music");
                Directory.CreateDirectory(location);
            }
        }
        SongDownloadLocation = location;
    }

    private static async Task SaveFoldersAsync()
    {
        var folderPaths = Data.MusicLibrary.Folders?.Select(f => f.Path).ToList();
        await ApplicationData.Current.LocalFolder.SaveAsync("MusicFolders", folderPaths);//	ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。SaveAsync("MusicFolders", folderPaths)：调用 SettingsStorageExtensions 类中的扩展方法 SaveAsync，将 folderPaths 列表保存到名为 "MusicFolders" 的文件中。
    }

    private async void SaveSongDownloadLocationAsync(string songDownloadLocation)
    {
        await _localSettingsService.SaveSettingAsync("SongDownloadLocation", songDownloadLocation);
    }

    private async void SaveSelectedFontAsync(string fontName)
    {
        await _localSettingsService.SaveSettingAsync("SelectedFont", fontName);
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

    private async void SaveLyricBackgroundVisibilityAsync(bool isLyricBackgroundVisible)
    {
        await _localSettingsService.SaveSettingAsync("IsLyricBackgroundVisible", isLyricBackgroundVisible);
    }
}
