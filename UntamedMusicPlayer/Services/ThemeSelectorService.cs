using Microsoft.UI.Xaml;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Services;

public sealed class ThemeSelectorService : IThemeSelectorService
{
    public AppTheme Theme
    {
        get => Settings.Theme;
        set => Settings.Theme = value;
    }

    public static bool IsDarkTheme =>
        ((FrameworkElement)App.MainWindow!.Content).ActualTheme == ElementTheme.Dark;

    public void Initialize()
    {
        SetRequestedThemeAsync();
    }

    public void SetThemeAsync(AppTheme theme)
    {
        Theme = theme;
        SetRequestedThemeAsync();
    }

    public void SetRequestedThemeAsync()
    {
        if (App.MainWindow!.Content is FrameworkElement rootElement)
        {
            var theme = Theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
            rootElement.RequestedTheme = theme;
            TitleBarHelper.UpdateTitleBar(App.MainWindow.AppWindow.TitleBar, theme);
        }
    }
}
