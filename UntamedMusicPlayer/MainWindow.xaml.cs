using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Messages;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.Views;
using Windows.System;
using WinRT.Interop;
using WinUIEx;
using ZLogger;

namespace UntamedMusicPlayer;

public sealed partial class MainWindow : WindowEx, IRecipient<LogMessage>
{
    private readonly ILogger _logger = LoggingService.CreateLogger<MainWindow>();
    private readonly InfoBarManager? _infoBarManager;

    // 热键 ID
    private const int HOTKEY_ID_VOLUME_UP = 1;
    private const int HOTKEY_ID_VOLUME_DOWN = 2;

    private nint _windowHandle;
    private nint _oldWndProc;

    // 必须保持委托引用防止 GC 回收
    private readonly WndProcDelegate _wndProcDelegate;

    public MainWindow()
    {
        InitializeComponent();
        Data.MainWindow = this;

        AppWindow.SetTaskbarIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        AppWindow.SetTitleBarIcon(
            Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico")
        );
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

        // 注册全局键盘和指针事件（用于ESC键和鼠标侧键导航）
        RootGrid.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnGlobalKeyDown), true);
        RootGrid.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler(OnGlobalPointerPressed),
            true
        );

        // 创建委托并保持引用（防止被 GC 回收）
        _wndProcDelegate = NewWindowProc;

        // 注册系统级全局热键
        Activated += MainWindow_Activated;
    }

    /// <summary>
    /// 窗口激活时注册全局热键
    /// </summary>
    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated; // 只在第一次激活时注册
        _windowHandle = WindowNative.GetWindowHandle(this); // 获取窗口句柄
        SubclassWindow(); // 子类化窗口以接收 WM_HOTKEY 消息
        RegisterGlobalHotKeys(); // 注册全局热键
    }

    // 窗口过程委托
    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    /// <summary>
    /// 子类化窗口以拦截消息
    /// </summary>
    private void SubclassWindow()
    {
        const int GWL_WNDPROC = -4;
        var newWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate); // 使用保存的委托引用
        _oldWndProc = ExternFunction.SetWindowLong(_windowHandle, GWL_WNDPROC, newWndProc); // 替换窗口过程
    }

    /// <summary>
    /// 新的窗口过程，用于拦截 WM_HOTKEY 消息
    /// </summary>
    private nint NewWindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == ExternFunction.WM_HOTKEY)
        {
            var hotkeyId = wParam.ToInt32();
            DispatcherQueue.TryEnqueue(() => // 在 UI 线程上处理热键
            {
                switch (hotkeyId)
                {
                    case HOTKEY_ID_VOLUME_UP: // Alt + Up: 增加音量
                        var currentVolumeUp = Data.PlayState.Volume;
                        Data.PlayState.Volume = Math.Min(100, currentVolumeUp + 5);
                        break;
                    case HOTKEY_ID_VOLUME_DOWN: // Alt + Down: 减少音量
                        var currentVolumeDown = Data.PlayState.Volume;
                        Data.PlayState.Volume = Math.Max(0, currentVolumeDown - 5);
                        break;
                }
            });
            return nint.Zero;
        }
        return ExternFunction.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam); // 调用原始窗口过程
    }

    /// <summary>
    /// 注册系统级全局热键
    /// </summary>
    private void RegisterGlobalHotKeys()
    {
        try
        {
            // 注册 Alt + Up (增加音量)
            var success1 = ExternFunction.RegisterHotKey(
                _windowHandle,
                HOTKEY_ID_VOLUME_UP,
                ExternFunction.MOD_ALT,
                ExternFunction.VK_UP
            );

            // 注册 Alt + Down (减少音量)
            var success2 = ExternFunction.RegisterHotKey(
                _windowHandle,
                HOTKEY_ID_VOLUME_DOWN,
                ExternFunction.MOD_ALT,
                ExternFunction.VK_DOWN
            );

            if (!success1 || !success2)
            {
                _logger.ZLogWarning($"注册全局热键失败。上: {success1}, 下: {success2}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"注册全局热键时出错");
        }
    }

    /// <summary>
    /// 注销系统级全局热键
    /// </summary>
    private void UnregisterGlobalHotKeys()
    {
        if (_windowHandle != nint.Zero)
        {
            ExternFunction.UnregisterHotKey(_windowHandle, HOTKEY_ID_VOLUME_UP);
            ExternFunction.UnregisterHotKey(_windowHandle, HOTKEY_ID_VOLUME_DOWN);
        }
    }

    /// <summary>
    /// 处理全局键盘按键事件（ESC键返回，Alt+上/下键调音量）
    /// </summary>
    private void OnGlobalKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape) // ESC 键返回
        {
            if (Data.RootPlayBarViewModel?.IsDetail == true)
            {
                Data.RootPlayBarViewModel.DetailModeUpdate();
            }
            else
            {
                Data.ShellPage?.GoBack();
            }
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// 处理全局鼠标按键事件（侧键返回）
    /// </summary>
    private void OnGlobalPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(RootGrid).Properties;
        if (properties.IsXButton1Pressed)
        {
            if (Data.RootPlayBarViewModel?.IsDetail == true)
            {
                Data.RootPlayBarViewModel.DetailModeUpdate();
            }
            else
            {
                Data.ShellPage?.GoBack();
            }
            e.Handled = true;
        }
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
            UnregisterGlobalHotKeys(); // 注销全局热键
            RootGrid.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnGlobalKeyDown));
            RootGrid.RemoveHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(OnGlobalPointerPressed)
            );
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
