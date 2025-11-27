using Microsoft.UI.Xaml;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Services;

public sealed class ThemeSelectorService : IThemeSelectorService
{
    public ElementTheme Theme
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

    public void SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;
        SetRequestedThemeAsync();
    }

    public void SetRequestedThemeAsync()
    {
        if (App.MainWindow!.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;
            TitleBarHelper.UpdateTitleBar(Theme);
        }
    }
}
