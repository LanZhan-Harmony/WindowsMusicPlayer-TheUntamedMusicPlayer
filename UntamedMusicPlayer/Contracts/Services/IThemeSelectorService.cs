using Microsoft.UI.Xaml;

namespace UntamedMusicPlayer.Contracts.Services;

public interface IThemeSelectorService
{
    ElementTheme Theme { get; set; }
    void Initialize();
    void SetThemeAsync(ElementTheme theme);
    void SetRequestedThemeAsync();
}
