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

    public DesktopLyricWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        // ��ȡ���ھ��
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // ��ȡ��Ļ��������С
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // ���㴰��λ�ã�ʹ��λ����Ļ�·�
        var x = (workArea.Width - 800) / 2; // ����
        var y = workArea.Height - 140; // �ײ�

        // ���ô���λ��
        appWindow.Move(new PointInt32(x, y));

        //���ô���Ϊ CompactOverlay ģʽ
        //appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        // ���ô��ڴ�С
        appWindow.Resize(new SizeInt32(800, 120));

        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
        }

        Closed += Window_Closed;
        //ViewModel = App.GetService<DesktopLyricViewModel>();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Data.RootPlayBarViewModel.IsDesktopLyricWindowStarted = false;
    }

    public void Dispose()
    {
    }
}
