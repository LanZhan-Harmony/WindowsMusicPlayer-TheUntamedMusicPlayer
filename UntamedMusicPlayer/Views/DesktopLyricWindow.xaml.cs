using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using WinRT.Interop;
using WinUIEx;
using ZLogger;
using static UntamedMusicPlayer.Helpers.ExternFunction;

namespace UntamedMusicPlayer.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DesktopLyricWindow>();
    private readonly nint _hWnd;
    private readonly int _maxTextBlockWidth;
    private readonly TextBlock _measureTextBlock = new()
    {
        FontSize = 32,
        FontFamily = Settings.FontFamily,
        MaxLines = 2,
    };
    private readonly Timer _updateTimer; // 用于周期性检查鼠标位置和置顶的计时器
    private readonly double _scaleFactor;
    private bool _isMouseOverBorder = false; // 检测鼠标是否在窗口上的变量
    private Storyboard? _currentStoryboard;
    private CancellationTokenSource? _sizeChangedCancellation;
    private delegate nint WndProcDelegate(nint hwnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;

    public DesktopLyricWindow()
    {
        Title = "DesktopLyricWindowTitle".GetLocalized();
        ExtendsContentIntoTitleBar = true;
        InitializeComponent();
        SetTitleBar(AnimatedBorder);

        _hWnd = WindowNative.GetWindowHandle(this); // 获取窗口句柄
        _scaleFactor = this.GetDpiForWindow() / 96.0;

        MakeWindowClickThrough(true);
        SetWindowProperty();
        SetTopmost(true);

        var workArea = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
            .WorkArea; // 获取屏幕工作区大小
        var screenWidth = workArea.Width;
        _maxTextBlockWidth = (int)(screenWidth / _scaleFactor) - 50;
        var screenHeight = workArea.Height;
        var y = screenHeight - screenHeight * 140 / 1080; // 计算窗口位置，使其位于屏幕下方
        this.SetWindowSize(screenWidth, (int)(55 * _scaleFactor));
        this.CenterOnScreen(null, null); // 设置窗口位置
        var currentPosition = AppWindow.Position;

        this.Move(currentPosition.X, y); // 将窗口移动到新的位置

        _updateTimer = new Timer(MousePositionTimer_Tick, null, 0, 250);

        Closed += Window_Closed;
    }

    private void SetWindowProperty()
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW; // 添加工具窗口样式
        exStyle &= ~WS_EX_APPWINDOW; // 移除应用窗口样式
        SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);

        const int GWL_STYLE = -16;
        const int WS_CAPTION = 0x00C00000;
        const int WS_SYSMENU = 0x00080000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_THICKFRAME = 0x00040000;
        var style = GetWindowLong(_hWnd, GWL_STYLE);
        style &= ~WS_CAPTION; // 去掉标题栏
        style &= ~WS_SYSMENU; // 去掉系统菜单（包含关闭菜单）
        style &= ~WS_MINIMIZEBOX; // 去掉最小化按钮
        style &= ~WS_MAXIMIZEBOX; // 去掉最大化按钮
        style &= ~WS_THICKFRAME; // 去掉可调整大小的边框
        SetWindowLong(_hWnd, GWL_STYLE, style);
        DisableMaximize();
    }

    private void DisableMaximize()
    {
        const int WM_NCLBUTTONDBLCLK = 0x00A3; // 非客户区左键双击
        const int WM_SYSCOMMAND = 0x0112; // 系统命令
        const int SC_MAXIMIZE = 0xF030; // 最大化命令
        const int GWLP_WNDPROC = -4; // 窗口过程指针

        var originalWndProc = GetWindowLong(_hWnd, GWLP_WNDPROC); // 获取原始窗口过程
        _wndProcDelegate = (hwnd, msg, wParam, lParam) => // 创建新的窗口过程委托
        {
            if (msg == WM_NCLBUTTONDBLCLK) // 阻止双击标题栏最大化
            {
                return nint.Zero; // 不处理双击事件
            }
            if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE) // 阻止系统最大化命令（如Win+上箭头等）
            {
                return nint.Zero; // 不处理最大化命令
            }
            return CallWindowProc(originalWndProc, hwnd, msg, wParam, lParam); // 调用原始窗口过程处理其他消息
        };

        // 设置新的窗口过程
        var funcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        SetWindowLong(_hWnd, GWLP_WNDPROC, funcPtr);
        GC.KeepAlive(_wndProcDelegate); // 防止委托被垃圾回收
    }

    private async void MousePositionTimer_Tick(object? _)
    {
        SetTopmost(true);

        GetCursorPos(out var inputPoint); // 获取当前输入位置

        var borderRect = await GetElementScreenRect(); // 获取AnimatedBorder在屏幕上的位置

        // 检查输入点是否在AnimatedBorder上
        var isOverBorder =
            inputPoint.X >= borderRect.Left
            && inputPoint.X <= borderRect.Right
            && inputPoint.Y >= borderRect.Top
            && inputPoint.Y <= borderRect.Bottom;

        if (isOverBorder != _isMouseOverBorder) // 如果鼠标状态改变
        {
            _isMouseOverBorder = isOverBorder;
            MakeWindowClickThrough(!isOverBorder);
        }
    }

    private async Task<RECT> GetElementScreenRect()
    {
        // 获取窗口在屏幕上的位置
        GetWindowRect(_hWnd, out var windowRect);

        var tcs = new TaskCompletionSource<RECT>();
        DispatcherQueue.TryEnqueue(() =>
        {
            // 获取元素在窗口中的位置
            var position = AnimatedBorder.TransformToVisual(null).TransformPoint(new Point(0, 0));

            // 考虑DPI缩放
            tcs.SetResult(
                new RECT
                {
                    Left = windowRect.Left + (int)(position.X * _scaleFactor),
                    Top = windowRect.Top + (int)(position.Y * _scaleFactor),
                    Right =
                        windowRect.Left
                        + (int)((position.X + AnimatedBorder.ActualWidth) * _scaleFactor),
                    Bottom =
                        windowRect.Top
                        + (int)((position.Y + AnimatedBorder.ActualHeight) * _scaleFactor),
                }
            );
        });
        return await tcs.Task;
    }

    /// <summary>
    /// 设置窗口是否点击穿透
    /// </summary>
    /// <param name="isClickThrough"></param>
    private void MakeWindowClickThrough(bool isClickThrough)
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TRANSPARENT = 0x00000020;

        var currentStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);

        if (isClickThrough)
        {
            SetWindowLong(_hWnd, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT); // 添加 WS_EX_TRANSPARENT 使窗口点击穿透
        }
        else
        {
            SetWindowLong(_hWnd, GWL_EXSTYLE, (currentStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT); // 移除 WS_EX_TRANSPARENT 使窗口可接收点击
        }
    }

    /// <summary>
    /// 设置窗口置顶
    /// </summary>
    /// <param name="value"></param>
    private void SetTopmost(bool value)
    {
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
        var position = value ? new nint(-1) : new nint(-2);
        SetWindowPos(_hWnd, position, 0, 0, 0, 0, flags);
    }

    private double GetTextBlockWidth(string? currentLyricContent)
    {
        if (string.IsNullOrEmpty(currentLyricContent))
        {
            return 100;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = _measureTextBlock.DesiredSize.Width;
        return Math.Min(width, _maxTextBlockWidth);
    }

    private double GetTextBlockHeight(string? currentLyricContent)
    {
        if (string.IsNullOrEmpty(currentLyricContent))
        {
            return 37;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var height = _measureTextBlock.DesiredSize.Height;
        return height;
    }

    private async void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _sizeChangedCancellation?.Cancel();
        _sizeChangedCancellation = new CancellationTokenSource();
        var cts = _sizeChangedCancellation;

        try
        {
            var currentWidth = AnimatedBorder.ActualWidth;
            var currentHeight = AnimatedBorder.ActualHeight;

            _currentStoryboard?.Stop();
            _currentStoryboard?.Children.Clear();
            _currentStoryboard = new Storyboard();

            // 使用当前实际大小作为动画起点，确保平滑过渡且不小于最小值
            var oldWidth = Math.Max(currentWidth, 150);
            var newWidth = Math.Max(e.NewSize.Width + 50, 150);
            var oldHeight = Math.Max(currentHeight, 55);
            var newHeight = e.NewSize.Height + 20;

            // 停止动画后立即设置显式大小，防止其回落到默认值
            AnimatedBorder.Width = oldWidth;
            AnimatedBorder.Height = oldHeight;

            if (Math.Abs(oldWidth - newWidth) > 1e-3)
            {
                var widthAmplitude = newWidth - (oldWidth - newWidth) * 0.275 > 0 ? 0.8 : 0.1;
                var widthAnimation = CreateDoubleAnimation(oldWidth, newWidth, widthAmplitude);
                Storyboard.SetTarget(widthAnimation, AnimatedBorder);
                Storyboard.SetTargetProperty(widthAnimation, "Width");
                _currentStoryboard.Children.Add(widthAnimation);
            }

            if (Math.Abs(oldHeight - newHeight) > 1e-3)
            {
                var heightAnimation = CreateDoubleAnimation(oldHeight, newHeight, 0.8);
                Storyboard.SetTarget(heightAnimation, AnimatedBorder);
                Storyboard.SetTargetProperty(heightAnimation, "Height");
                _currentStoryboard.Children.Add(heightAnimation);
            }

            if (_currentStoryboard.Children.Count > 0)
            {
                var heightDiff = newHeight - oldHeight;
                if (heightDiff > 1e-3)
                {
                    var maxWindowHeight = newHeight - (oldHeight - newHeight) * 0.275;
                    ResizeWindowKeepingCenter(_maxTextBlockWidth, maxWindowHeight + 5);
                }

                _currentStoryboard.Begin();

                if (Math.Abs(heightDiff) > 1e-3)
                {
                    await Task.Delay(300, cts.Token);
                    ResizeWindowKeepingCenter(_maxTextBlockWidth, newHeight + 5);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"调整灵动词岛宽度时发生错误");
        }
    }

    /// <summary>
    /// 调整窗口大小并保持水平中心位置不变
    /// </summary>
    /// <param name="newWidthLogical">新宽度（逻辑像素）</param>
    /// <param name="newHeightLogical">新高度（逻辑像素）</param>
    private void ResizeWindowKeepingCenter(double newWidthLogical, double newHeightLogical)
    {
        var newWidth = (int)(newWidthLogical * _scaleFactor);
        var newHeight = (int)(newHeightLogical * _scaleFactor);

        // 获取当前窗口位置和大小
        GetWindowRect(_hWnd, out var currentRect);
        var currentWidth = currentRect.Right - currentRect.Left;

        // 计算当前窗口水平中心
        var centerX = currentRect.Left + (currentWidth >> 1);

        // 计算新的左上角位置，使水平中心保持不变，垂直位置（顶部）保持不变
        var newLeft = centerX - (newWidth >> 1);
        var newTop = currentRect.Top;

        // 使用 SetWindowPos 同时设置位置和大小
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        SetWindowPos(
            _hWnd,
            nint.Zero,
            newLeft,
            newTop,
            newWidth,
            newHeight,
            SWP_NOZORDER | SWP_NOACTIVATE
        );
    }

    private static DoubleAnimation CreateDoubleAnimation(double from, double to, double amplitude)
    {
        return new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(300),
            EnableDependentAnimation = true,
            EasingFunction = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = amplitude,
            },
        };
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Data.RootPlayBarViewModel?.IsDesktopLyricWindowStarted = false;
        _updateTimer.Dispose();
    }

    public void Dispose()
    {
        Data.DesktopLyricWindow = null;
    }
}
