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

        // ��ȡ���ھ��
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        SetTitleBar(Draggable);

        // ��ȡ��Ļ��������С
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // ��Ļ����
        var screenWidth = workArea.Width;
        var screenHeight = workArea.Height;

        // ���ڳ���
        var windowWidth = screenWidth * 1000 / 1920;
        var windowHeight = screenHeight * 100 / 1080;

        // ���㴰��λ�ã�ʹ��λ����Ļ�·�
        var x = (screenWidth - windowWidth) / 2; // ����
        var y = screenHeight - screenHeight * 140 / 1080; // �ײ�

        // ���ô���λ��
        appWindow.Move(new PointInt32(x, y));

        //���ô���Ϊ CompactOverlay ģʽ
        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        // ���ô��ڴ�С
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
