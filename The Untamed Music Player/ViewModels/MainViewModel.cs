using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.UI;
using WinRT;
using WinUIEx;

namespace The_Untamed_Music_Player.ViewModels;

public class MainViewModel
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private IDynamicBackgroundService _dynamicBackgroundService = null!;

    private readonly MainWindow _mainWindow;

    public bool IsDarkTheme { get; set; }

    public MainViewModel()
    {
        _mainWindow = Data.MainWindow ?? new();
        IsDarkTheme =
            ((FrameworkElement)_mainWindow.Content).ActualTheme == ElementTheme.Dark
            || (
                ((FrameworkElement)_mainWindow.Content).ActualTheme == ElementTheme.Default
                && App.Current.RequestedTheme == ApplicationTheme.Dark
            );
        InitializeDynamicBackground();
        ((FrameworkElement)_mainWindow.Content).ActualThemeChanged += Window_ThemeChanged;
        Data.MainViewModel = this;
    }

    public void InitializeDynamicBackground()
    {
        _dynamicBackgroundService = App.GetService<IDynamicBackgroundService>();
        // 初始化动态背景服务，使用根网格作为目标元素
        _dynamicBackgroundService.Initialize(_mainWindow.GetBackgroundGrid());

        // 如果当前已有正在播放的歌曲，立即更新背景
        _ = _dynamicBackgroundService.UpdateBackgroundAsync();
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        IsDarkTheme =
            ((FrameworkElement)_mainWindow.Content).ActualTheme == ElementTheme.Dark
            || (
                ((FrameworkElement)_mainWindow.Content).ActualTheme == ElementTheme.Default
                && App.Current.RequestedTheme == ApplicationTheme.Dark
            );
    }

    /// <summary>
    /// 清理动态背景服务
    /// </summary>
    public void CleanupDynamicBackgroundService()
    {
        _dynamicBackgroundService?.Dispose();
    }
}
