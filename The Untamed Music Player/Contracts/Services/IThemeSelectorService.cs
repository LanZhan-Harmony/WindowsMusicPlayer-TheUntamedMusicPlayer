using Microsoft.UI.Xaml;

namespace The_Untamed_Music_Player.Contracts.Services;

public interface IThemeSelectorService
{
    ElementTheme Theme { get; set; }
    void Initialize();
    void SetThemeAsync(ElementTheme theme);
    void SetRequestedThemeAsync();
}
