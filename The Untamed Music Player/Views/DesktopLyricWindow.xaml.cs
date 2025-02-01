using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
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

        /*if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = ElementTheme.Dark;
        }*/

        Closed += Window_Closed;
        LyricFrame.Navigate(typeof(DesktopLyricPage), null, new DrillInNavigationTransitionInfo());
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
}
