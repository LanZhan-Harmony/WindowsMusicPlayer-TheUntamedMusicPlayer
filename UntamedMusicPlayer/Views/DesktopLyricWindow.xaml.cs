using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Services;
using Windows.Foundation;
using WinRT.Interop;
using WinUIEx;
using ZLogger;

namespace UntamedMusicPlayer.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DesktopLyricWindow>();
    private readonly int _maxTextBlockWidth;
    private readonly TextBlock _measureTextBlock = new()
    {
        FontSize = 32,
        FontFamily = Settings.FontFamily,
        MaxLines = 2,
    };
    private CancellationTokenSource? _sizeChangedCancellation;
    private readonly Compositor? _compositor;
    private readonly Visual? _borderVisual;
    private readonly DesktopLyricWindowHelper _windowHelper;

    public DesktopLyricWindow()
    {
        Title = "DesktopLyricWindowTitle".GetLocalized();
        ExtendsContentIntoTitleBar = true;
        InitializeComponent();
        SetTitleBar(AnimatedBorder);

        var hWnd = WindowNative.GetWindowHandle(this);
        var scaleFactor = this.GetDpiForWindow() / 96.0;

        // 初始化 Composition API
        _borderVisual = ElementCompositionPreview.GetElementVisual(AnimatedBorder);
        _compositor = _borderVisual.Compositor;

        var workArea = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)
            .WorkArea;
        var screenWidth = workArea.Width;
        var screenHeight = workArea.Height;
        _maxTextBlockWidth = (int)(screenWidth / scaleFactor) - 50;

        // 初始化窗口 Win32 辅助（点击穿透、置顶、拖拽、触摸）
        _windowHelper = new DesktopLyricWindowHelper(
            hWnd,
            scaleFactor,
            AnimatedBorder,
            DispatcherQueue,
            screenWidth,
            screenHeight,
            _logger
        );
        _windowHelper.Setup();

        // 测量两行文字的高度
        _measureTextBlock.Text = "测试文字\n第二行";
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var windowHeight = (int)((_measureTextBlock.DesiredSize.Height + 30) * scaleFactor);

        // 距工作区底部保持 40 逻辑像素（按 DPI 缩放），适配不同分辨率
        var y = screenHeight - windowHeight - (int)(40 * scaleFactor);
        this.SetWindowSize(screenWidth, windowHeight);
        this.CenterOnScreen(null, null);
        this.Move(AppWindow.Position.X, y);

        Closed += Window_Closed;
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

        try
        {
            if (_compositor is null || _borderVisual is null)
            {
                return;
            }

            var currentWidth = AnimatedBorder.ActualWidth;
            var currentHeight = AnimatedBorder.ActualHeight;

            // 使用当前实际大小作为动画起点，确保平滑过渡且不小于最小值
            var oldWidth = Math.Max(currentWidth, 150);
            var newWidth = Math.Max(e.NewSize.Width + 50, 150);
            var oldHeight = Math.Max(currentHeight, 55);
            var newHeight = e.NewSize.Height + 20;

            var widthDiff = Math.Abs(oldWidth - newWidth);
            var heightDiff = Math.Abs(oldHeight - newHeight);

            if (widthDiff < 1e-3 && heightDiff < 1e-3)
            {
                return;
            }

            // 先设置目标大小
            AnimatedBorder.Width = newWidth;
            AnimatedBorder.Height = newHeight;

            // 计算缩放比例（从旧大小到新大小）
            var scaleX = (float)(oldWidth / newWidth);
            var scaleY = (float)(oldHeight / newHeight);

            // 设置缩放中心点为元素中心
            _borderVisual.CenterPoint = new Vector3((float)newWidth / 2, (float)newHeight / 2, 0);

            // 创建弹簧动画
            var springAnimation = _compositor.CreateSpringVector3Animation();
            springAnimation.FinalValue = new Vector3(1, 1, 1);
            springAnimation.InitialValue = new Vector3(scaleX, scaleY, 1);
            springAnimation.DampingRatio = 0.5f;
            springAnimation.Period = TimeSpan.FromMilliseconds(50);

            // 停止之前的动画并启动新动画
            _borderVisual.StopAnimation("Scale");
            _borderVisual.StartAnimation("Scale", springAnimation);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"调整灵动词岛宽度时发生错误");
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Data.RootPlayBarViewModel?.IsDesktopLyricWindowStarted = false;
        _windowHelper.Dispose();
    }

    public void Dispose()
    {
        Data.DesktopLyricWindow = null;
    }
}
