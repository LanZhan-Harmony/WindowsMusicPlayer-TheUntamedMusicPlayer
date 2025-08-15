using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Messages;

namespace The_Untamed_Music_Player.Helpers;

/// <summary>
/// InfoBar管理器，提供高级的InfoBar显示功能
/// </summary>
public partial class InfoBarManager : IDisposable
{
    private readonly InfoBar _infoBar;
    private readonly Storyboard _showInfoBarStoryboard;
    private readonly Storyboard _hideInfoBarStoryboard;
    private readonly Queue<LogMessage> _messageQueue = [];
    private bool _isDisplaying = false;
    private DispatcherTimer? _autoCloseTimer;

    public InfoBarManager(
        InfoBar infoBar,
        Storyboard showInfoBarStoryboard,
        Storyboard hideInfoBarStoryBoard
    )
    {
        _infoBar = infoBar;
        _showInfoBarStoryboard = showInfoBarStoryboard;
        _hideInfoBarStoryboard = hideInfoBarStoryBoard;
        _infoBar.Closed += OnInfoBarClosed;
    }

    /// <summary>
    /// 显示日志消息
    /// </summary>
    /// <param name="logMessage">日志消息</param>
    /// <param name="autoCloseSeconds">自动关闭时间（秒）</param>
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

    private void DisplayMessage(LogMessage logMessage, int autoCloseSeconds)
    {
        try
        {
            _isDisplaying = true;
            _autoCloseTimer?.Stop();

            _infoBar.Title = logMessage.Message;

            _infoBar.IsOpen = true;
            _infoBar.Opacity = 0;
            _infoBar.Visibility = Visibility.Visible;
            _showInfoBarStoryboard.Begin();
            if (autoCloseSeconds > 0)
            {
                _autoCloseTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(autoCloseSeconds),
                };

                _autoCloseTimer.Tick += async (_, _) =>
                {
                    _autoCloseTimer.Stop();
                    await HideInfoBar();
                };

                _autoCloseTimer.Start();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示InfoBar消息时出错: {ex.Message}");
            _isDisplaying = false;
        }
    }

    private async Task HideInfoBar()
    {
        _hideInfoBarStoryboard.Begin();
        await Task.Delay(100);
        _infoBar.Visibility = Visibility.Collapsed;
        _infoBar.IsOpen = false;
        _isDisplaying = false;

        // 处理队列中的下一条消息
        if (_messageQueue.Count > 0)
        {
            var nextMessage = _messageQueue.Dequeue();
            await Task.Delay(200);
            DisplayMessage(nextMessage, 5);
        }
    }

    /// <summary>
    /// InfoBar关闭事件处理
    /// </summary>
    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        _isDisplaying = false;
        if (args.Reason == InfoBarCloseReason.CloseButton)
        {
            _infoBar.Visibility = Visibility.Collapsed;
            _messageQueue.Clear();
        }
    }

    public void Dispose()
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
        _infoBar.Closed -= OnInfoBarClosed;
        _messageQueue.Clear();
    }
}
