using System.Runtime.InteropServices;
using Linearstar.Windows.RawInput;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using ZLogger;
using static UntamedMusicPlayer.Helpers.ExternFunction;

namespace UntamedMusicPlayer.Helpers;

/// <summary>
/// 封装桌面歌词窗口的所有 Win32 窗口管理逻辑：
/// 点击穿透、置顶、WndProc 子类化、Raw Input 触摸拖拽。
/// </summary>
public sealed partial class DesktopLyricWindowHelper : IDisposable
{
    private readonly nint _hWnd;
    private readonly double _scaleFactor;
    private readonly FrameworkElement _captionElement;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly ILogger _logger;

    // ── 拖拽状态（三种互斥） ──
    private int _dragOffsetX;
    private int _dragOffsetY;
    private bool _isWndProcDragging; // WndProc 鼠标拖拽（窗口可交互时）
    private bool _isTouchDragging; // Raw Input 触摸拖拽
    private int _touchContactId = -1;

    // ── 穿透 / 钩子 ──
    private bool _isMouseOverBorder;
    private RECT _cachedBorderRect;
    private nint _previousForegroundWindow;
    private bool _isTemporarilyHidden;
    private CancellationTokenSource? _temporaryHideCts;

    // ── WndProc ──
    private delegate nint WndProcDelegate(nint hwnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;

    // ── 定时器 ──
    private readonly Timer _updateTimer;

    public DesktopLyricWindowHelper(
        nint hWnd,
        double scaleFactor,
        FrameworkElement captionElement,
        DispatcherQueue dispatcherQueue,
        int screenWidth,
        int screenHeight,
        ILogger logger
    )
    {
        _hWnd = hWnd;
        _scaleFactor = scaleFactor;
        _captionElement = captionElement;
        _dispatcherQueue = dispatcherQueue;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _logger = logger;

        _updateTimer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 一次性初始化窗口属性、WndProc、钩子、Raw Input 和定时器。
    /// </summary>
    public void Setup()
    {
        MakeClickThrough(true);
        SetWindowStyles();
        SetupWndProc();
        SetTopmost(true);
        RegisterRawInput();
        _updateTimer.Change(0, 100);
    }

    // ═══════════════════════════════════════════════════
    //  公开方法
    // ═══════════════════════════════════════════════════

    public void MakeClickThrough(bool value)
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TRANSPARENT = 0x00000020;

        var style = GetWindowLong(_hWnd, GWL_EXSTYLE);
        style = value
            ? style | WS_EX_LAYERED | WS_EX_TRANSPARENT
            : (style | WS_EX_LAYERED) & ~WS_EX_TRANSPARENT;
        SetWindowLong(_hWnd, GWL_EXSTYLE, style);
    }

    public void SetTopmost(bool value)
    {
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        SetWindowPos(
            _hWnd,
            value ? new nint(-1) : new nint(-2),
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
        );
    }

    // ═══════════════════════════════════════════════════
    //  窗口样式
    // ═══════════════════════════════════════════════════

    private void SetWindowStyles()
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int WS_EX_APPWINDOW = 0x00040000;
        var exStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);
        SetWindowLong(_hWnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

        const int GWL_STYLE = -16;
        const int WS_CAPTION = 0x00C00000;
        const int WS_SYSMENU = 0x00080000;
        const int WS_MINIMIZEBOX = 0x00020000;
        const int WS_MAXIMIZEBOX = 0x00010000;
        const int WS_THICKFRAME = 0x00040000;
        var style = GetWindowLong(_hWnd, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_THICKFRAME);
        SetWindowLong(_hWnd, GWL_STYLE, style);
    }

    // ═══════════════════════════════════════════════════
    //  WndProc 子类化（鼠标拖拽 + 阻止最大化 + Raw Input）
    // ═══════════════════════════════════════════════════

    private void SetupWndProc()
    {
        const int WM_NCLBUTTONDBLCLK = 0x00A3;
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int HTCAPTION = 2;
        const int WM_INPUT = 0x00FF;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_LBUTTONUP = 0x0202;
        const int WM_CAPTURECHANGED = 0x0215;
        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MAXIMIZE = 0xF030;
        const int GWLP_WNDPROC = -4;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint MoveFlags = SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE;

        var originalWndProc = GetWindowLong(_hWnd, GWLP_WNDPROC);

        _wndProcDelegate = (hwnd, msg, wParam, lParam) =>
        {
            if (msg == WM_INPUT)
            {
                HandleRawInput(lParam);
            }

            if (msg == WM_NCLBUTTONDBLCLK)
            {
                _ = HideWindowTemporarilyAsync();
                return nint.Zero;
            }

            if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE)
            {
                return nint.Zero;
            }

            // 鼠标拖拽（窗口可交互时，拦截标题栏左键按下以绕过 SC_MOVE）
            if (msg == WM_NCLBUTTONDOWN && wParam.ToInt32() == HTCAPTION)
            {
                CapturePreviousForegroundWindow();
                GetCursorPos(out var cursor);
                GetWindowRect(hwnd, out var windowRect);
                _dragOffsetX = cursor.X - windowRect.Left;
                _dragOffsetY = cursor.Y - windowRect.Top;
                _isWndProcDragging = true;
                SetCapture(hwnd);
                return nint.Zero;
            }

            if (_isWndProcDragging)
            {
                if (msg == WM_MOUSEMOVE)
                {
                    GetCursorPos(out var cursor);
                    SetWindowPos(
                        hwnd,
                        nint.Zero,
                        cursor.X - _dragOffsetX,
                        cursor.Y - _dragOffsetY,
                        0,
                        0,
                        MoveFlags
                    );
                    return nint.Zero;
                }
                if (msg == WM_LBUTTONUP)
                {
                    _isWndProcDragging = false;
                    ReleaseCapture();
                    return nint.Zero;
                }
                if (msg == WM_CAPTURECHANGED)
                {
                    _isWndProcDragging = false;
                    return nint.Zero;
                }
            }

            return CallWindowProc(originalWndProc, hwnd, msg, wParam, lParam);
        };

        SetWindowLong(_hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        GC.KeepAlive(_wndProcDelegate);
    }

    // ═══════════════════════════════════════════════════
    //  Raw Input 触摸拖拽
    // ═══════════════════════════════════════════════════

    private void RegisterRawInput()
    {
        RawInputDevice.RegisterDevice(
            HidUsageAndPage.TouchScreen,
            RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify,
            _hWnd
        );
        RawInputDevice.RegisterDevice(
            HidUsageAndPage.Pen,
            RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify,
            _hWnd
        );
    }

    private void HandleRawInput(nint lParam)
    {
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint MoveFlags = SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE;

        try
        {
            var data = RawInputData.FromHandle(lParam);
            if (data is not RawInputDigitizerData digitizerData)
            {
                return;
            }

            var displayOrientation = GetCurrentDisplayOrientation();

            foreach (var contact in digitizerData.Contacts)
            {
                var maxX = contact.MaxX;
                var maxY = contact.MaxY;
                if (maxX <= 0 || maxY <= 0)
                {
                    continue;
                }

                var (screenX, screenY) = ConvertRawInputToScreenPoint(
                    contact.X,
                    contact.Y,
                    maxX,
                    maxY,
                    displayOrientation
                );

                // 触摸拖拽进行中
                if (_isTouchDragging && contact.Identifier == _touchContactId)
                {
                    if (
                        contact.Kind
                        is RawInputDigitizerContactKind.Finger
                            or RawInputDigitizerContactKind.Pen
                    )
                    {
                        SetWindowPos(
                            _hWnd,
                            nint.Zero,
                            screenX - _dragOffsetX,
                            screenY - _dragOffsetY,
                            0,
                            0,
                            MoveFlags
                        );
                    }
                    else
                    {
                        _isTouchDragging = false;
                        _touchContactId = -1;
                    }
                    return;
                }

                // 新的触摸按下 → 检测是否在 Border 上
                if (
                    !_isTouchDragging
                    && contact.Kind
                        is RawInputDigitizerContactKind.Finger
                            or RawInputDigitizerContactKind.Pen
                )
                {
                    var rect = _cachedBorderRect;
                    if (
                        screenX >= rect.Left
                        && screenX <= rect.Right
                        && screenY >= rect.Top
                        && screenY <= rect.Bottom
                    )
                    {
                        GetWindowRect(_hWnd, out var windowRect);
                        _dragOffsetX = screenX - windowRect.Left;
                        _dragOffsetY = screenY - windowRect.Top;
                        _touchContactId = contact.Identifier ?? -1;
                        _isTouchDragging = true;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"处理RawInput触摸数据时发生错误");
        }
    }

    private static uint GetCurrentDisplayOrientation()
    {
        var devMode = new DEVMODE { dmSize = (ushort)Marshal.SizeOf<DEVMODE>() };
        return EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode)
            ? devMode.dmDisplayOrientation
            : DMDO_DEFAULT;
    }

    private (int X, int Y) ConvertRawInputToScreenPoint(
        int rawX,
        int rawY,
        int maxX,
        int maxY,
        uint displayOrientation
    )
    {
        var normalizedX = Math.Clamp(rawX / (double)maxX, 0d, 1d);
        var normalizedY = Math.Clamp(rawY / (double)maxY, 0d, 1d);

        var (mappedX, mappedY) = displayOrientation switch
        {
            DMDO_90 => (1d - normalizedY, normalizedX),
            DMDO_180 => (1d - normalizedX, 1d - normalizedY),
            DMDO_270 => (normalizedY, 1d - normalizedX),
            _ => (normalizedX, normalizedY),
        };

        var screenX = (int)Math.Round(mappedX * _screenWidth, MidpointRounding.AwayFromZero);
        var screenY = (int)Math.Round(mappedY * _screenHeight, MidpointRounding.AwayFromZero);
        return (Math.Clamp(screenX, 0, _screenWidth), Math.Clamp(screenY, 0, _screenHeight));
    }

    // ═══════════════════════════════════════════════════
    //  定时器（置顶 + 缓存 Border 屏幕坐标）
    // ═══════════════════════════════════════════════════

    private async void TimerTick(object? _)
    {
        SetTopmost(true);

        GetCursorPos(out var inputPoint); // 获取当前输入位置
        _cachedBorderRect = await GetElementScreenRect(); // 获取AnimatedBorder在屏幕上的位置

        // 检查输入点是否在AnimatedBorder上
        var isOverBorder =
            inputPoint.X >= _cachedBorderRect.Left
            && inputPoint.X <= _cachedBorderRect.Right
            && inputPoint.Y >= _cachedBorderRect.Top
            && inputPoint.Y <= _cachedBorderRect.Bottom;

        if (isOverBorder != _isMouseOverBorder) // 如果鼠标状态改变
        {
            _isMouseOverBorder = isOverBorder;

            if (isOverBorder)
            {
                CapturePreviousForegroundWindow();
            }
            else
            {
                RestorePreviousForegroundWindow();
            }

            MakeWindowClickThrough(!isOverBorder);
        }
    }

    private void CapturePreviousForegroundWindow()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != nint.Zero && foregroundWindow != _hWnd)
        {
            _previousForegroundWindow = foregroundWindow;
        }
    }

    private void RestorePreviousForegroundWindow()
    {
        if (_isTemporarilyHidden)
        {
            return;
        }
        if (_previousForegroundWindow == nint.Zero || _previousForegroundWindow == _hWnd)
        {
            return;
        }

        var currentForegroundWindow = GetForegroundWindow();
        if (currentForegroundWindow != nint.Zero && currentForegroundWindow != _hWnd)
        {
            return;
        }
        _ = SetForegroundWindow(_previousForegroundWindow);
    }

    private async Task HideWindowTemporarilyAsync()
    {
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint SWP_HIDEWINDOW = 0x0080;

        _temporaryHideCts?.Cancel();
        _temporaryHideCts?.Dispose();

        _temporaryHideCts = new CancellationTokenSource();
        var token = _temporaryHideCts.Token;

        CapturePreviousForegroundWindow();
        _isTemporarilyHidden = true;
        _isMouseOverBorder = false;
        MakeWindowClickThrough(true);

        _ = SetWindowPos(
            _hWnd,
            nint.Zero,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_HIDEWINDOW
        );

        RestorePreviousForegroundWindow();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _ = SetWindowPos(
                _hWnd,
                nint.Zero,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW
            );
            SetTopmost(true);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isTemporarilyHidden = false;
        }
    }

    private async Task<RECT> GetElementScreenRect()
    {
        GetWindowRect(_hWnd, out var windowRect);

        var tcs = new TaskCompletionSource<RECT>();
        _dispatcherQueue.TryEnqueue(() =>
        {
            var position = _captionElement.TransformToVisual(null).TransformPoint(new Point(0, 0));
            tcs.SetResult(
                new RECT
                {
                    Left = windowRect.Left + (int)(position.X * _scaleFactor),
                    Top = windowRect.Top + (int)(position.Y * _scaleFactor),
                    Right =
                        windowRect.Left
                        + (int)((position.X + _captionElement.ActualWidth) * _scaleFactor),
                    Bottom =
                        windowRect.Top
                        + (int)((position.Y + _captionElement.ActualHeight) * _scaleFactor),
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

    // ═══════════════════════════════════════════════════
    //  Dispose
    // ═══════════════════════════════════════════════════

    public void Dispose()
    {
        _temporaryHideCts?.Cancel();
        _temporaryHideCts?.Dispose();
        _updateTimer.Dispose();
    }
}
