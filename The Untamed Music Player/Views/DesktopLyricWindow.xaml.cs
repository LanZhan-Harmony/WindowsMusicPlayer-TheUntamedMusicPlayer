using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : Window, IDisposable
{
    public DesktopLyricViewModel ViewModel
    {
        get;
    }

    /*private Compositor _compositor;
    private Visual _borderVisual;*/

    public DesktopLyricWindow()
    {
        ViewModel = App.GetService<DesktopLyricViewModel>();
        InitializeComponent();

        // 获取窗口句柄
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        SetTitleBar(Draggable);

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
        var x = (screenWidth - windowWidth) / 2; // 居中
        var y = screenHeight - screenHeight * 140 / 1080; // 底部

        // 设置窗口位置
        appWindow.Move(new PointInt32(x, y));

        //设置窗口为 CompactOverlay 模式
        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        // 设置窗口大小
        appWindow.Resize(new SizeInt32(windowWidth, windowHeight));

        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
        }

        /*if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = ElementTheme.Dark;
        }*/

        Closed += Window_Closed;

        /*_compositor = ElementCompositionPreview.GetElementVisual(Content).Compositor;
        _borderVisual = ElementCompositionPreview.GetElementVisual(AnimatedBorder);*/
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (Data.RootPlayBarViewModel != null)
        {
            Data.RootPlayBarViewModel.IsDesktopLyricWindowStarted = false;
        }
    }

    public void Dispose()
    {
    }

    /*private void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        *//*var widthAnimation = _compositor.CreateScalarKeyFrameAnimation();
        widthAnimation.InsertKeyFrame(1.0f, (float)LyricContent.ActualWidth);
        widthAnimation.Duration = TimeSpan.FromMilliseconds(300);

        var heightAnimation = _compositor.CreateScalarKeyFrameAnimation();
        heightAnimation.InsertKeyFrame(1.0f, (float)LyricContent.ActualHeight);
        heightAnimation.Duration = TimeSpan.FromMilliseconds(300);

        _borderVisual.StartAnimation("Size.X", widthAnimation);
        _borderVisual.StartAnimation("Size.Y", heightAnimation);*//*
    }*/
}
