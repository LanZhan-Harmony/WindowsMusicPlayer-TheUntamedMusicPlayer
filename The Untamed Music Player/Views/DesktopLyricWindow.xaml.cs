using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Labs.WinUI.MarqueeTextRns;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.Foundation;
using Windows.System;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    private readonly nint _hWnd;

    // 用于周期性检查鼠标位置和置顶的计时器
    private DispatcherTimer? _updateTimer250ms;

    // 检测鼠标是否在我们的窗口上的变量
    private bool _isMouseOverBorder = false;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private Storyboard? _currentStoryboard;
    private readonly TextBlock _measureTextBlock = new()
    {
        FontSize = 32,
        FontFamily = Data.SelectedFont
    };

    public DesktopLyricViewModel ViewModel
    {
        get;
    }

    public DesktopLyricWindow()
    {
        ViewModel = App.GetService<DesktopLyricViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(Draggable);
        Title = "DesktopLyricWindowTitle".GetLocalized();

        // 获取窗口句柄
        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed; // 去除右上角三键

        MakeWindowClickThrough(_hWnd, true);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW;  // 添加工具窗口样式
        exStyle &= ~WS_EX_APPWINDOW;  // 移除应用窗口样式
        _ = SetWindowLong(_hWnd, GWL_EXSTYLE, exStyle);

        SetTopmost(_hWnd, true);

        // 获取屏幕工作区大小
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // 屏幕长宽
        var screenWidth = workArea.Width;
        var screenHeight = workArea.Height;

        // 窗口长宽
        var windowWidth = screenWidth * 1000 / 1920;
        var windowHeight = screenHeight * 100 / 1080;

        // 计算窗口位置，使其位于屏幕下方
        var y = screenHeight - screenHeight * 140 / 1080; // 底部

        // 设置窗口位置
        this.SetWindowSize(1000, 100);
        this.CenterOnScreen(null, null);

        var currentPosition = appWindow.Position;
        // 将窗口移动到新的位置
        this.Move(currentPosition.X, y);

        InitMousePositionTimer();
        Closed += Window_Closed;
    }

    // P/Invoke 声明
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private void InitMousePositionTimer()
    {
        _updateTimer250ms = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _updateTimer250ms.Tick += MousePositionTimer_Tick;
        _updateTimer250ms.Start();
    }

    private void MousePositionTimer_Tick(object? sender, object e)
    {
        SetTopmost(_hWnd, true);

        // 获取当前鼠标位置
        GetCursorPos(out var mousePoint);

        // 获取窗口句柄
        var hWnd = WindowNative.GetWindowHandle(this);

        // 获取AnimatedBorder在屏幕上的位置
        var borderRect = GetElementScreenRect(AnimatedBorder);

        // 检查鼠标是否在AnimatedBorder上
        var isOverBorder = mousePoint.x >= borderRect.left
            && mousePoint.x <= borderRect.right
            && mousePoint.y >= borderRect.top
            && mousePoint.y <= borderRect.bottom;

        // 如果鼠标状态改变
        if (isOverBorder != _isMouseOverBorder)
        {
            _isMouseOverBorder = isOverBorder;
            MakeWindowClickThrough(hWnd, !isOverBorder);
        }
    }

    private RECT GetElementScreenRect(FrameworkElement element)
    {
        var hWnd = WindowNative.GetWindowHandle(this);

        // 获取元素在窗口中的位置
        var transform = element.TransformToVisual(null);
        var position = transform.TransformPoint(new Point(0, 0));

        // 获取窗口在屏幕上的位置
        GetWindowRect(hWnd, out var windowRect);

        return new RECT
        {
            left = windowRect.left + (int)position.X,
            top = windowRect.top + (int)position.Y,
            right = windowRect.left + (int)(position.X + element.ActualWidth),
            bottom = windowRect.top + (int)(position.Y + element.ActualHeight)
        };
    }

    // 设置窗口是否点击穿透
    private static void MakeWindowClickThrough(IntPtr hwnd, bool isClickThrough)
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TRANSPARENT = 0x00000020;

        var currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (isClickThrough)
        {
            // 添加 WS_EX_TRANSPARENT 使窗口点击穿透
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
        else
        {
            // 移除 WS_EX_TRANSPARENT 使窗口可接收点击
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, (currentStyle | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT);
        }
    }

    // 设置窗口置顶
    private static void SetTopmost(IntPtr hwnd, bool value)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;

        var position = value ? new IntPtr(-1) : new IntPtr(-2);

        SetWindowPos(hwnd, position, 0, 0, 0, 0, flags);
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (Data.RootPlayBarViewModel is not null)
        {
            Data.RootPlayBarViewModel.IsDesktopLyricWindowStarted = false;
        }
        // 停止计时器
        if (_updateTimer250ms != null)
        {
            _updateTimer250ms.Stop();
            _updateTimer250ms = null;
        }
    }

    public void Dispose()
    {
    }

    private void LyricContent_Loaded(object sender, RoutedEventArgs e)
    {
        var textBlock = new TextBlock
        {
            Text = "TEST测试",
            FontSize = 32,
            FontFamily = Data.SelectedFont
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
            Task.Delay(500).ContinueWith(_ =>
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
                EasingFunction = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 0.8
                }
            };
            Storyboard.SetTarget(widthAnimation, AnimatedBorder);
            Storyboard.SetTargetProperty(widthAnimation, "Width");
            _currentStoryboard = new Storyboard();
            _currentStoryboard.Children.Add(widthAnimation);
            _currentStoryboard.Begin();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}