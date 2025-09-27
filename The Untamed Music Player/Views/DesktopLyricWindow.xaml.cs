using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using Windows.Foundation;
using Windows.UI.WindowManagement;
using WinRT.Interop;
using WinUIEx;
using ZLogger;
using static The_Untamed_Music_Player.Helpers.ExternFunction;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DesktopLyricWindow>();
    private readonly nint _hWnd;

    private readonly int _maxTextBlockWidth;
    private bool _isMouseOverBorder = false; // 检测鼠标是否在窗口上的变量
    private DispatcherTimer? _updateTimer250ms; // 用于周期性检查鼠标位置和置顶的计时器

    private Storyboard? _currentStoryboard;
    private readonly TextBlock _measureTextBlock = new()
    {
        FontSize = 32,
        FontFamily = Settings.FontFamily,
        MaxLines = 2,
    };

    public DesktopLyricWindow()
    {
        Title = "DesktopLyricWindowTitle".GetLocalized();
        ExtendsContentIntoTitleBar = true;
        InitializeComponent();
        SetTitleBar(AnimatedBorder);

        _hWnd = WindowNative.GetWindowHandle(this); // 获取窗口句柄

        MakeWindowClickThrough(true);
        SetWindowProperty();
        SetTopmost(true);

        var workArea = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
            .WorkArea; // 获取屏幕工作区大小
        var screenWidth = workArea.Width;
        _maxTextBlockWidth = screenWidth - 50;
        var screenHeight = workArea.Height;
        var y = screenHeight - screenHeight * 140 / 1080; // 计算窗口位置，使其位于屏幕下方
        this.SetWindowSize(screenWidth, 60);
        this.CenterOnScreen(null, null); // 设置窗口位置
        var currentPosition = AppWindow.Position;

        // 将窗口移动到新的位置
        this.Move(currentPosition.X, y);

        InitMousePositionTimer();
        Closed += Window_Closed;
        Data.MusicPlayer.NotifyLyricContentChanged();
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
        style &= ~WS_THICKFRAME; // 去掉可调整大小的边框（如果不需要保留调整大小）
        SetWindowLong(_hWnd, GWL_STYLE, style);
    }

    private void InitMousePositionTimer()
    {
        _updateTimer250ms = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _updateTimer250ms.Tick += MousePositionTimer_Tick;
        _updateTimer250ms.Start();
    }

    private void MousePositionTimer_Tick(object? sender, object e)
    {
        SetTopmost(true);

        // 获取当前鼠标位置
        GetCursorPos(out var mousePoint);
        // 获取AnimatedBorder在屏幕上的位置
        var borderRect = GetElementScreenRect(AnimatedBorder);

        // 检查鼠标是否在AnimatedBorder上
        var isOverBorder =
            mousePoint.X >= borderRect.Left
            && mousePoint.X <= borderRect.Right
            && mousePoint.Y >= borderRect.Top
            && mousePoint.Y <= borderRect.Bottom;

        if (isOverBorder != _isMouseOverBorder) // 如果鼠标状态改变
        {
            _isMouseOverBorder = isOverBorder;
            MakeWindowClickThrough(!isOverBorder);
        }
    }

    private RECT GetElementScreenRect(FrameworkElement element)
    {
        // 获取DPI缩放因子
        var dpi = this.GetDpiForWindow();
        var scaleFactor = dpi / 96.0; // 96是标准DPI

        // 获取元素在窗口中的位置
        var transform = element.TransformToVisual(null);
        var position = transform.TransformPoint(new Point(0, 0));

        // 获取窗口在屏幕上的位置
        GetWindowRect(_hWnd, out var windowRect);

        // 考虑DPI缩放
        return new RECT
        {
            Left = windowRect.Left + (int)(position.X * scaleFactor),
            Top = windowRect.Top + (int)(position.Y * scaleFactor),
            Right = windowRect.Left + (int)((position.X + element.ActualWidth) * scaleFactor),
            Bottom = windowRect.Top + (int)((position.Y + element.ActualHeight) * scaleFactor),
        };
    }

    // 设置窗口是否点击穿透
    private void MakeWindowClickThrough(bool isClickThrough)
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TRANSPARENT = 0x00000020;

        var currentStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);

        if (isClickThrough)
        {
            // 添加 WS_EX_TRANSPARENT 使窗口点击穿透
            SetWindowLong(_hWnd, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
        else
        {
            // 移除 WS_EX_TRANSPARENT 使窗口可接收点击
            SetWindowLong(_hWnd, GWL_EXSTYLE, (currentStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT);
        }
    }

    // 设置窗口置顶
    private void SetTopmost(bool value)
    {
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
        var position = value ? new nint(-1) : new nint(-2);
        SetWindowPos(_hWnd, position, 0, 0, 0, 0, flags);
    }

    private double GetTextBlockWidth(string currentLyricContent)
    {
        if (currentLyricContent == "")
        {
            return 100;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = _measureTextBlock.DesiredSize.Width;
        return Math.Min(width, _maxTextBlockWidth);
    }

    private double GetTextBlockHeight(string currentLyricContent)
    {
        if (currentLyricContent == "")
        {
            return 37;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var height = _measureTextBlock.DesiredSize.Height;
        return height;
    }

    private void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            _currentStoryboard?.Stop();
            _currentStoryboard?.Children.Clear();
            _currentStoryboard = new Storyboard();

            var oldWidth = e.PreviousSize.Width + 50;
            var newWidth = Math.Max(e.NewSize.Width + 50, 150);
            var oldHeight = e.PreviousSize.Height + 20;
            var newHeight = e.NewSize.Height + 20;

            if (Math.Abs(oldWidth - newWidth) > 1e-3)
            {
                var widthAmplitude = oldWidth - newWidth > 800 ? 0.1 : 0.8; // 避免宽度减小到负数
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
                if (newHeight - oldHeight > 1e-3) // 如果新高度更大, 先调整高度, 再启动动画
                {
                    this.SetWindowSize(_maxTextBlockWidth, newHeight + 5);
                }
                _currentStoryboard.Begin();
                if (oldHeight - newHeight > 1e-3) // 如果新高度更小, 动画结束后再调整高度
                {
                    this.SetWindowSize(_maxTextBlockWidth, newHeight + 5);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"调整灵动词岛宽度时发生错误");
        }
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
        _updateTimer250ms?.Stop();
        _updateTimer250ms = null;
    }

    public void Dispose() { }
}
