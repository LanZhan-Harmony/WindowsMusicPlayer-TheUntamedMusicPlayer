using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Messages;

namespace The_Untamed_Music_Player.Services;

/// <summary>
/// InfoBar管理器，提供高级的InfoBar显示功能
/// </summary>
public class InfoBarManager
{
    private readonly InfoBar _infoBar;
    private DispatcherTimer? _autoCloseTimer;
    private readonly Queue<LogMessage> _messageQueue = new();
    private bool _isDisplaying = false;

    /// <summary>
    /// 初始化InfoBar管理器
    /// </summary>
    /// <param name="infoBar">要管理的InfoBar控件</param>
    public InfoBarManager(InfoBar infoBar)
    {
        _infoBar = infoBar ?? throw new ArgumentNullException(nameof(infoBar));

        // 监听InfoBar关闭事件
        _infoBar.Closed += OnInfoBarClosed;
    }

    /// <summary>
    /// 显示日志消息
    /// </summary>
    /// <param name="logMessage">日志消息</param>
    /// <param name="autoCloseSeconds">自动关闭时间（秒），0表示不自动关闭</param>
    public void ShowMessage(LogMessage logMessage, int autoCloseSeconds = 5)
    {
        // 如果当前正在显示消息，加入队列
        if (_isDisplaying)
        {
            _messageQueue.Enqueue(logMessage);
            return;
        }

        DisplayMessage(logMessage, autoCloseSeconds);
    }

    /// <summary>
    /// 立即显示消息（会中断当前显示的消息）
    /// </summary>
    /// <param name="logMessage">日志消息</param>
    /// <param name="autoCloseSeconds">自动关闭时间（秒）</param>
    public void ShowMessageImmediately(LogMessage logMessage, int autoCloseSeconds = 5)
    {
        // 停止当前计时器
        StopAutoCloseTimer();

        // 立即显示新消息
        DisplayMessage(logMessage, autoCloseSeconds);
    }

    /// <summary>
    /// 手动关闭InfoBar
    /// </summary>
    public void Close()
    {
        StopAutoCloseTimer();
        _infoBar.IsOpen = false;
    }

    /// <summary>
    /// 清空消息队列
    /// </summary>
    public void ClearQueue()
    {
        _messageQueue.Clear();
    }

    /// <summary>
    /// 获取队列中的消息数量
    /// </summary>
    public int QueueCount => _messageQueue.Count;

    /// <summary>
    /// 显示消息的内部方法
    /// </summary>
    /// <param name="logMessage">日志消息</param>
    /// <param name="autoCloseSeconds">自动关闭时间</param>
    private void DisplayMessage(LogMessage logMessage, int autoCloseSeconds)
    {
        try
        {
            _isDisplaying = true;

            // 设置InfoBar属性
            _infoBar.Title = GetInfoBarTitle(logMessage.Level);
            _infoBar.Message = logMessage.Message;
            _infoBar.Severity = GetInfoBarSeverity(logMessage.Level);
            _infoBar.IsOpen = true;

            // 设置自动关闭计时器
            if (autoCloseSeconds > 0)
            {
                SetupAutoCloseTimer(autoCloseSeconds);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示InfoBar消息时出错: {ex.Message}");
            _isDisplaying = false;
        }
    }

    /// <summary>
    /// 设置自动关闭计时器
    /// </summary>
    /// <param name="seconds">秒数</param>
    private void SetupAutoCloseTimer(int seconds)
    {
        StopAutoCloseTimer();

        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };

        _autoCloseTimer.Tick += (sender, e) =>
        {
            StopAutoCloseTimer();
            _infoBar.IsOpen = false;
        };

        _autoCloseTimer.Start();
    }

    /// <summary>
    /// 停止自动关闭计时器
    /// </summary>
    private void StopAutoCloseTimer()
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
    }

    /// <summary>
    /// InfoBar关闭事件处理
    /// </summary>
    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        _isDisplaying = false;
        StopAutoCloseTimer();

        // 显示队列中的下一条消息
        if (_messageQueue.Count > 0)
        {
            var nextMessage = _messageQueue.Dequeue();

            // 延迟一点显示下一条消息，避免闪烁
            var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };

            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                DisplayMessage(nextMessage, 5);
            };

            delayTimer.Start();
        }
    }

    /// <summary>
    /// 获取InfoBar标题
    /// </summary>
    private static string GetInfoBarTitle(LogLevel level) =>
        level switch
        {
            LogLevel.Error => "错误",
            LogLevel.Critical => "严重错误",
            LogLevel.Warning => "警告",
            _ => "通知",
        };

    /// <summary>
    /// 获取InfoBar严重级别
    /// </summary>
    private static InfoBarSeverity GetInfoBarSeverity(LogLevel level) =>
        level switch
        {
            LogLevel.Error => InfoBarSeverity.Error,
            LogLevel.Critical => InfoBarSeverity.Error,
            LogLevel.Warning => InfoBarSeverity.Warning,
            _ => InfoBarSeverity.Informational,
        };

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StopAutoCloseTimer();
        _infoBar.Closed -= OnInfoBarClosed;
        ClearQueue();
    }
}
