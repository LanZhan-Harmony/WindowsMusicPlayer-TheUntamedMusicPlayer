using Microsoft.UI.Xaml;

namespace The_Untamed_Music_Player.Contracts.Services;
public interface IThemeSelectorService
{
    ElementTheme Theme
    {
        get;
    }

    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    Task SetRequestedThemeAsync();
}
