using System.Numerics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Messages;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI.ViewManagement;

namespace The_Untamed_Music_Player;

public sealed partial class MainWindow : WindowEx, IRecipient<LogMessage>
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private InfoBarManager? _infoBarManager;

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

        // 初始化InfoBar管理器
        _infoBarManager = new InfoBarManager(
            ErrorInfoBar,
            ShowInfoBarStoryboard,
            HideInfoBarStoryboard
        );

        // 注册日志消息接收
        StrongReferenceMessenger.Default.Register(this);

        ErrorInfoBar.Translation += new Vector3(0, 0, 40);
    }

    /// <summary>
    /// 接收日志消息并在InfoBar中显示
    /// </summary>
    /// <param name="message">日志消息</param>
    public void Receive(LogMessage message)
    {
        // 只处理Error和Critical级别的日志
        if (message.Level < LogLevel.Error)
        {
            return;
        }

        // 确保在UI线程上执行
        dispatcherQueue.TryEnqueue(() =>
        {
            _infoBarManager?.ShowMessage(message);
        });
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
    /// 窗口关闭时的清理工作
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        StrongReferenceMessenger.Default.Unregister<LogMessage>(this);
        _infoBarManager?.Dispose();
        _infoBarManager = null;
    }
}
