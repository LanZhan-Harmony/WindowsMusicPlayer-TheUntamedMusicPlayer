using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using static UntamedMusicPlayer.Helpers.ExternFunction;

namespace UntamedMusicPlayer.Helpers;

public sealed partial class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    public static void UpdateTitleBar(AppWindowTitleBar titleBar, ElementTheme theme)
    {
        if (theme == ElementTheme.Default)
        {
            var uiSettings = new UISettings();
            var background = uiSettings.GetColorValue(UIColorType.Background);
            theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
        }

        titleBar.ButtonForegroundColor = theme switch
        {
            ElementTheme.Dark => Colors.White,
            ElementTheme.Light => Colors.Black,
            _ => Colors.Transparent,
        };

        titleBar.ButtonHoverForegroundColor = theme switch
        {
            ElementTheme.Dark => Colors.White,
            ElementTheme.Light => Colors.Black,
            _ => Colors.Transparent,
        };

        titleBar.ButtonHoverBackgroundColor = theme switch
        {
            ElementTheme.Dark => Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
            ElementTheme.Light => Color.FromArgb(0x33, 0x00, 0x00, 0x00),
            _ => Colors.Transparent,
        };

        titleBar.ButtonPressedBackgroundColor = theme switch
        {
            ElementTheme.Dark => Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF),
            ElementTheme.Light => Color.FromArgb(0x66, 0x00, 0x00, 0x00),
            _ => Colors.Transparent,
        };

        titleBar.BackgroundColor = Colors.Transparent;

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        if (hwnd == GetActiveWindow())
        {
            SendMessage(hwnd, WMACTIVATE, WAINACTIVE, nint.Zero);
            SendMessage(hwnd, WMACTIVATE, WAACTIVE, nint.Zero);
        }
        else
        {
            SendMessage(hwnd, WMACTIVATE, WAACTIVE, nint.Zero);
            SendMessage(hwnd, WMACTIVATE, WAINACTIVE, nint.Zero);
        }
    }
}
