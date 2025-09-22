using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Services;

public class ThemeSelectorService : IThemeSelectorService
{
    public ElementTheme Theme
    {
        get;
        set
        {
            field = value;
            Settings.Theme = value;
        }
    }

    public static bool IsDarkTheme =>
        ((FrameworkElement)App.MainWindow!.Content).ActualTheme == ElementTheme.Dark
        || (
            ((FrameworkElement)App.MainWindow!.Content).ActualTheme == ElementTheme.Default
            && App.Current.RequestedTheme == ApplicationTheme.Dark
        );

    public void Initialize()
    {
        Theme = Settings.Theme;
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
