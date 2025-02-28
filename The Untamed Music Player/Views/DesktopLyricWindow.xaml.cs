using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.Foundation;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    // 定义需要的Win32 API和常量
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

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

        // 获取窗口句柄
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        /*var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW;  // 添加工具窗口样式
        exStyle &= ~WS_EX_APPWINDOW;  // 移除应用窗口样式
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);*/

        ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        SetTitleBar(Draggable);
        Title = "DesktopLyricWindowTitle".GetLocalized();

        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

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
        DLW.SetWindowSize(1000, 100);
        DLW.CenterOnScreen(null, null);

        var currentPosition = appWindow.Position;
        // 将窗口移动到新的位置
        DLW.Move(currentPosition.X, y);
        //DLW.SetIsAlwaysOnTop(true);

        /*if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = ElementTheme.Dark;
        }*/

        Closed += Window_Closed;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (Data.RootPlayBarViewModel is not null)
        {
            Data.RootPlayBarViewModel.IsDesktopLyricWindowStarted = false;
        }
    }

    public void Dispose()
    {
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
        Dispose();
        Data.RootPlayBarViewModel!.IsDesktopLyricWindowStarted = false;
    }

    private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var button = grid?.FindName("CloseButton") as Button;
        if (button is not null)
        {
            button.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var button = grid?.FindName("CloseButton") as Button;
        if (button is not null)
        {
            button.Visibility = Visibility.Collapsed;
        }
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
        return Math.Min(_measureTextBlock.DesiredSize.Width, 700);
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
