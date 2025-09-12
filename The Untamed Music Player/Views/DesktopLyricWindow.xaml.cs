using CommunityToolkit.Labs.WinUI.MarqueeTextRns;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using Windows.Foundation;
using Windows.System;
using WinRT.Interop;
using WinUIEx;
using ZLogger;
using static The_Untamed_Music_Player.Helpers.ExternFunction;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DesktopLyricWindow>();
    private readonly nint _hWnd;

    private bool _isDragging = false; // 检测是否在拖动的变量
    private bool _isMouseOverBorder = false; // 检测鼠标是否在窗口上的变量

    private POINT _lastPointerPosition;
    private DispatcherTimer? _updateTimer250ms; // 用于周期性检查鼠标位置和置顶的计时器

    private Storyboard? _currentStoryboard;
    private readonly TextBlock _measureTextBlock = new()
    {
        FontSize = 32,
        FontFamily = Data.SelectedFontFamily,
    };

    public DesktopLyricViewModel ViewModel { get; }

    public DesktopLyricWindow()
    {
        ViewModel = App.GetService<DesktopLyricViewModel>();
        InitializeComponent();
        Title = "DesktopLyricWindowTitle".GetLocalized();

        _hWnd = WindowNative.GetWindowHandle(this); // 获取窗口句柄

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        AppWindow.SetPresenter(presenter);

        MakeWindowClickThrough(true);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW; // 添加工具窗口样式
        exStyle &= ~WS_EX_APPWINDOW; // 移除应用窗口样式
        _ = SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);

        SetTopmost(true);

        var workArea = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
            .WorkArea; // 获取屏幕工作区大小
        var screenHeight = workArea.Height;
        var y = screenHeight - screenHeight * 140 / 1080; // 计算窗口位置，使其位于屏幕下方

        this.SetWindowSize(1000, 100);
        this.CenterOnScreen(null, null); // 设置窗口位置

        var currentPosition = AppWindow.Position;

        // 将窗口移动到新的位置
        this.Move(currentPosition.X, y);

        InitMousePositionTimer();
        Closed += Window_Closed;
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

        // 如果鼠标状态改变
        if (isOverBorder != _isMouseOverBorder)
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
            _ = SetWindowLong(_hWnd, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
        else
        {
            // 移除 WS_EX_TRANSPARENT 使窗口可接收点击
            _ = SetWindowLong(
                _hWnd,
                GWL_EXSTYLE,
                (currentStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT
            );
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

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Data.RootPlayBarViewModel?.IsDesktopLyricWindowStarted = false;
        if (_updateTimer250ms is not null)
        {
            _updateTimer250ms.Stop();
            _updateTimer250ms = null;
        }
    }

    public void Dispose() { }

    private void AnimatedBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        GetCursorPos(out var point);
        _lastPointerPosition = point;
        (sender as Border)!.CapturePointer(e.Pointer);
    }

    private void AnimatedBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        GetCursorPos(out var point);

        // 计算移动差值
        var deltaX = point.X - _lastPointerPosition.X;
        var deltaY = point.Y - _lastPointerPosition.Y;

        // 更新窗口位置
        this.Move(AppWindow.Position.X + deltaX, AppWindow.Position.Y + deltaY);

        _lastPointerPosition = point;
    }

    private void AnimatedBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            (sender as Border)!.ReleasePointerCapture(e.Pointer);
        }
    }

    private void LyricContent_Loaded(object sender, RoutedEventArgs e)
    {
        var textBlock = new TextBlock
        {
            Text = "TEST测试",
            FontSize = 32,
            FontFamily = Data.SelectedFontFamily,
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        (sender as MarqueeText)!.Height = textBlock.DesiredSize.Height;
    }

    private double GetTextBlockWidth(string currentLyricContent)
    {
        LyricContent.StopMarquee();
        if (currentLyricContent == "")
        {
            return 140;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = _measureTextBlock.DesiredSize.Width;
        if (width > 700)
        {
            // 在UI线程上延迟0.5秒后调用 StartMarquee
            Task.Delay(500)
                .ContinueWith(_ =>
                {
                    // 确保在UI线程上下文中执行
                    DispatcherQueue.TryEnqueue(() => LyricContent.StartMarquee());
                });
        }
        return Math.Min(width, 700);
    }

    private void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            _currentStoryboard?.Stop();
            _currentStoryboard?.Children.Clear();
            var newWidth = e.NewSize.Width;
            var widthAnimation = new DoubleAnimation
            {
                From = e.PreviousSize.Width + 50,
                To = newWidth > 140 ? newWidth + 50 : 190,
                Duration = TimeSpan.FromMilliseconds(300),
                EnableDependentAnimation = true,
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.8 },
            };
            Storyboard.SetTarget(widthAnimation, AnimatedBorder);
            Storyboard.SetTargetProperty(widthAnimation, "Width");
            _currentStoryboard = new Storyboard();
            _currentStoryboard.Children.Add(widthAnimation);
            _currentStoryboard.Begin();
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"调整灵动词岛宽度时发生错误");
        }
    }
}
