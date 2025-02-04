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
using Windows.Graphics;
using WinRT.Interop;

namespace The_Untamed_Music_Player.Views;

public sealed partial class DesktopLyricWindow : WindowEx, IDisposable
{
    // ������Ҫ��Win32 API�ͳ���
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

        // ��ȡ���ھ��
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        /*var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW;  // ��ӹ��ߴ�����ʽ
        exStyle &= ~WS_EX_APPWINDOW;  // �Ƴ�Ӧ�ô�����ʽ
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);*/

        ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        SetTitleBar(Draggable);
        Title = "DesktopLyricWindowTitle".GetLocalized();

        appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);

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
        var y = screenHeight - screenHeight * 140 / 1080; // �ײ�

        // ���ô���λ��
        DLW.SetWindowSize(1000, 100);
        DLW.CenterOnScreen(null, null);

        var currentPosition = appWindow.Position;
        // �������ƶ����µ�λ��
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
        if (Data.RootPlayBarViewModel != null)
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
        if (button != null)
        {
            button.Visibility = Visibility.Visible;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        var button = grid?.FindName("CloseButton") as Button;
        if (button != null)
        {
            button.Visibility = Visibility.Collapsed;
        }
    }

    private double GetTextBlockWidth(string currentLyricContent)
    {
        LyricContent.StopMarquee();
        if (currentLyricContent == "")
        {
            return 100;
        }
        _measureTextBlock.Text = currentLyricContent;
        _measureTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Min(_measureTextBlock.DesiredSize.Width, 700);
    }

    private void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            if (_currentStoryboard != null)
            {
                _currentStoryboard.Stop();
                _currentStoryboard.Children.Clear();
            }
            var widthAnimation = new DoubleAnimation
            {
                From = e.PreviousSize.Width + 50,
                To = e.NewSize.Width + 50,
                Duration = TimeSpan.FromMilliseconds(300),
                EnableDependentAnimation = true,
                EasingFunction = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 1
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
