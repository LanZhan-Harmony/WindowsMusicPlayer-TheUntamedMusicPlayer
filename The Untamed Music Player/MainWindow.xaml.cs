using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI.ViewManagement;

namespace The_Untamed_Music_Player;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private readonly IDynamicBackgroundService _dynamicBackgroundService;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/WindowIcon.ico"));
        Title = "AppDisplayName".GetLocalized();
        ExtendsContentIntoTitleBar = true;

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;

        Data.MainWindow = this;

        ShellFrame.Navigate(typeof(ShellPage));
        ViewModel = App.GetService<MainViewModel>();

        // 初始化动态背景服务
        _dynamicBackgroundService = App.GetService<IDynamicBackgroundService>();

        // 在窗口加载完成后初始化动态背景
        MainWindow_Loaded();
        Closed += MainWindow_Closed;
    }

    /// <summary>
    /// 获取导航页(ShellFrame)
    /// </summary>
    /// <returns></returns>
    public Frame GetShellFrame()
    {
        return ShellFrame;
    }

    /// <summary>
    /// 处理在应用程序打开时主题改变时正确更新标题按钮颜色
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // 这个调用来自线程外，因此我们需要将其调度到当前应用程序的线程
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    private void MainWindow_Loaded()
    {
        // 初始化动态背景服务，使用根网格作为目标元素
        _dynamicBackgroundService.Initialize(BackgroundGrid);

        // 如果当前已有正在播放的歌曲，立即更新背景
        _ = _dynamicBackgroundService.UpdateBackgroundAsync();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // 清理动态背景服务
        _dynamicBackgroundService.Dispose();
    }
}
