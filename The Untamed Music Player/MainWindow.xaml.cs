using System.Numerics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI.ViewManagement;
using ZLogger;

namespace The_Untamed_Music_Player;

public sealed partial class MainWindow : WindowEx, IRecipient<LogMessage>
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private readonly ILogger _logger = LoggingService.CreateLogger<MainWindow>();
    private InfoBarManager? _infoBarManager;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        Title = "AppDisplayName".GetLocalized();
        ExtendsContentIntoTitleBar = true;

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;

        Data.MainWindow = this;

        ShellFrame.Navigate(typeof(ShellPage));
        ViewModel = App.GetService<MainViewModel>();

        // 初始化InfoBar管理器
        _infoBarManager = new InfoBarManager(
            ErrorInfoBar,
            SendFeedbackButton,
            ShowInfoBarStoryboard,
            HideInfoBarStoryboard
        );

        // 注册日志消息接收
        StrongReferenceMessenger.Default.Register(this);

        ErrorInfoBar.Translation += new Vector3(0, 0, 40);

        // 注册AppWindow.Closing事件来处理窗口关闭
        AppWindow.Closing += AppWindow_Closing;
    }

    /// <summary>
    /// 接收日志消息并在InfoBar中显示
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Receive(LogMessage message)
    {
        // 只处理Error和Critical级别的日志
        if (message.Level >= LogLevel.Error)
        {
            // 确保在UI线程上执行
            dispatcherQueue.TryEnqueue(() =>
            {
                _infoBarManager?.ShowMessage(message);
            });
        }
    }

    /// <summary>
    /// 获取导航页(ShellFrame)
    /// </summary>
    /// <returns></returns>
    public Frame GetShellFrame()
    {
        return ShellFrame;
    }

    public Grid GetBackgroundGrid()
    {
        return BackgroundGrid;
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

    /// <summary>
    /// 处理AppWindow关闭请求 - 在窗口实际关闭前处理数据保存
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            args.Cancel = true;
            await Data.MusicPlayer.SaveCurrentStateAsync();
            await Data.PlaylistLibrary.SaveLibraryAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"保存应用程序数据失败");
        }
        finally
        {
            Close();
        }
    }

    /// <summary>
    /// 窗口关闭时的清理工作
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            Data.MusicPlayer.Dispose();
            ViewModel.CleanupDynamicBackgroundService(); // 清理背景服务
            ViewModel.CleanupSystemBackdrop(); // 清理系统背景
            Data.DesktopLyricWindow?.Close(); // 关闭桌面歌词窗口
            Data.DesktopLyricWindow?.Dispose();
            StrongReferenceMessenger.Default.Unregister<LogMessage>(this); // 清理消息接收
            _infoBarManager?.Dispose(); // 清理InfoBar管理器
            _infoBarManager = null;
            LoggingService.Shutdown(); // 关闭日志服务
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"清理资源失败");
        }
    }
}
