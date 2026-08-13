using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Contracts.Services;

public interface IThemeSelectorService
{
    AppTheme Theme { get; set; }
    void Initialize();
    void SetThemeAsync(AppTheme theme);
    void SetRequestedThemeAsync();
}
