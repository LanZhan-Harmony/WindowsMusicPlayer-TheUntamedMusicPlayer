using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace The_Untamed_Music_Player.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INotifyPropertyChanged
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private static readonly ResourceLoader _resourceLoader = new();
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [ObservableProperty]
    public ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public Visibility EmptyFolderMessageVisibility => Data.MusicLibrary.Folders?.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

    private List<string> _fonts = [];
    public List<string> Fonts
    {
        get => _fonts;
        set => _fonts = value;
    }

    private FontFamily _selectedFont = new("Microsoft YaHei");
    public FontFamily SelectedFont
    {
        get => _selectedFont;
        set
        {
            _selectedFont = value;
            OnPropertyChanged(nameof(SelectedFont));
            SaveSelectedFontAsync(value.Source); // 保存字体设置
        }
    }

    private List<string> _materials = [.. "Settings_Materials".GetLocalized().Split(", ")];
    public List<string> Materials
    {
        get => _materials;
        set => _materials = value;
    }

    private byte _selectedMaterial = Data.MainWindow.SelectedMaterial;
    public byte SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            _selectedMaterial = value;
            OnPropertyChanged(nameof(SelectedMaterial));
            ChangeMaterial(SelectedMaterial);
            SaveSelectedMaterialAsync(value);
        }
    }

    private bool _isLyricBackgroundVisible;
    public bool IsLyricBackgroundVisible
    {
        get => _isLyricBackgroundVisible;
        set
        {
            _isLyricBackgroundVisible = value;
            OnPropertyChanged(nameof(IsLyricBackgroundVisible));
            SaveLyricBackgroundVisibilityAsync(value);
        }
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        LoadFonts();
        LoadSelectedFontAsync();
        LoadLyricBackgroundVisibilityAsync();
        Data.SettingsViewModel = this;
    }

    public async void PickMusicFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var openPicker = new FolderPicker();

        var window = App.MainWindow;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.FileTypeFilter.Add("*");

        var folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            if (!Data.MusicLibrary.Folders.Any(f => f.Path == folder.Path))
            {
                Data.MusicLibrary.Folders.Add(folder);
                OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
                await SaveFoldersAsync();
                await Data.MusicLibrary.LoadLibraryAgain(); // 重新加载音乐库
            }
        }
    }

    public void RemoveMusicFolderButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is StorageFolder folder)
        {
            RemoveMusicFolder(folder);
        }
    }

    private async void RemoveMusicFolder(StorageFolder folder)
    {
        Data.MusicLibrary.Folders?.Remove(folder);
        OnPropertyChanged(nameof(EmptyFolderMessageVisibility));
        await SaveFoldersAsync();
        await Data.MusicLibrary.LoadLibraryAgain();
    }

    public async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await Data.MusicLibrary.LoadLibraryAgain();
    }

    public void LoadFonts()
    {
        var fontFamilies = Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies();
        Fonts = [.. fontFamilies.OrderBy(f => f)];
    }

    public void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedFont)
        {
            SelectedFont = new FontFamily(selectedFont);
        }
    }

    public void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedMaterial)
        {
            SelectedMaterial = (byte)Materials.IndexOf(selectedMaterial);
            if (Data.MainWindow != null)
            {
                Data.MainWindow.SelectedMaterial = SelectedMaterial;
            }
        }
    }

    public void ChangeMaterial(byte material)
    {
        if (Data.MainWindow?.SelectedMaterial == SelectedMaterial)
        {
            return;
        }
        try
        {
            switch (material)
            {
                case 0:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () => Data.MainWindow?.TrySetNoneBackdrop());
                    break;
                case 1:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetMicaBackdrop(false);
                    });
                    break;
                case 2:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetMicaBackdrop(true);
                    });
                    break;
                case 3:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetDesktopAcrylicBackdrop();
                    });
                    break;
                case 4:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetAcrylicBackdrop(false);
                    });
                    break;
                case 5:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetAcrylicBackdrop(true);
                    });
                    break;
                case 6:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetBlurBackdrop();
                    });
                    break;
                case 7:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetTransparentBackdrop();
                    });
                    break;
                case 8:
                    Data.MainWindow?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                    {
                        Data.MainWindow?.TrySetNoneBackdrop();
                        Data.MainWindow?.TrySetAnimatedBackdrop();
                    });
                    break;
                default:
                    break;
            }
        }
        catch { }
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

    private static async Task SaveFoldersAsync()
    {
        var folderPaths = Data.MusicLibrary.Folders?.Select(f => f.Path).ToList();//Folders.Select(f => f.Path)：使用 LINQ 查询从 Folders 集合中提取每个 StorageFolder 的路径（Path 属性）。.ToList()：将提取的路径集合转换为 List<string>。
        await ApplicationData.Current.LocalFolder.SaveAsync("MusicFolders", folderPaths);//	ApplicationData.Current.LocalFolder：获取应用程序的本地存储文件夹。SaveAsync("MusicFolders", folderPaths)：调用 SettingsStorageExtensions 类中的扩展方法 SaveAsync，将 folderPaths 列表保存到名为 "MusicFolders" 的文件中。
    }

    private async void LoadSelectedFontAsync()
    {
        var fontName = await _localSettingsService.ReadSettingAsync<string>("SelectedFont");
        if (!string.IsNullOrEmpty(fontName))
        {
            SelectedFont = new FontFamily(fontName);
        }
    }

    private async void LoadLyricBackgroundVisibilityAsync()
    {
        var isLyricBackgroundVisible = await _localSettingsService.ReadSettingAsync<bool>("IsLyricBackgroundVisible");
        IsLyricBackgroundVisible = isLyricBackgroundVisible;
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

    public void MaterialComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.SelectedIndex = SelectedMaterial;
        }
    }

    public void LyricBackgroundCheckBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            toggleSwitch.IsOn = IsLyricBackgroundVisible;
        }
    }

    private async void SaveSelectedFontAsync(string fontName)
    {
        await _localSettingsService.SaveSettingAsync("SelectedFont", fontName);
    }

    private async void SaveSelectedMaterialAsync(byte material)
    {
        await _localSettingsService.SaveSettingAsync("SelectedMaterial", material);
    }

    private async void SaveLyricBackgroundVisibilityAsync(bool isLyricBackgroundVisible)
    {
        await _localSettingsService.SaveSettingAsync("IsLyricBackgroundVisible", isLyricBackgroundVisible);
    }
}
