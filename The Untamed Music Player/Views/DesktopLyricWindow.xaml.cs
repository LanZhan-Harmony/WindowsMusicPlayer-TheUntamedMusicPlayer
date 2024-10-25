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
        // 获取窗口句柄
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // 获取屏幕工作区大小
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // 计算窗口位置，使其位于屏幕下方
        var x = (workArea.Width - 800) / 2; // 居中
        var y = workArea.Height - 140; // 底部

        // 设置窗口位置
        appWindow.Move(new PointInt32(x, y));

        //设置窗口为 CompactOverlay 模式
        //appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        // 设置窗口大小
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
