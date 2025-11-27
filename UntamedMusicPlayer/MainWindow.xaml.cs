using System.Numerics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.Views;
using WinUIEx;
using ZLogger;

namespace UntamedMusicPlayer;

public sealed partial class MainWindow : WindowEx, IRecipient<LogMessage>
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MainWindow>();
    private readonly InfoBarManager? _infoBarManager;

    public MainWindow()
    {
        InitializeComponent();
        Data.MainWindow = this;

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        Title = "AppDisplayName".GetLocalized();
        ExtendsContentIntoTitleBar = true;

        ShellFrame.Navigate(typeof(ShellPage));

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
            DispatcherQueue.TryEnqueue(() =>
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
    /// 处理AppWindow关闭请求 - 在窗口实际关闭前处理数据保存
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            args.Cancel = true;
            await Data.MusicPlayer.SaveStateAsync();
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
            Data.DesktopLyricWindow?.Close(); // 关闭桌面歌词窗口
            Data.DesktopLyricWindow?.Dispose();
            StrongReferenceMessenger.Default.Unregister<LogMessage>(this); // 清理消息接收
            _infoBarManager?.Dispose(); // 清理InfoBar管理器
            App.GetService<IMaterialSelectorService>().Dispose();
            App.GetService<IDynamicBackgroundService>().Dispose();
            LoggingService.Shutdown(); // 关闭日志服务
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"清理资源失败");
        }
    }
}
