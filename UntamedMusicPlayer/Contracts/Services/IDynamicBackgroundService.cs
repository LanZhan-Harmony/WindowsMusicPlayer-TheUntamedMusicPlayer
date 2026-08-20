using Microsoft.UI.Xaml;
using Windows.UI;

namespace UntamedMusicPlayer.Contracts.Services;

/// <summary>
/// 动态背景服务接口
/// </summary>
public interface IDynamicBackgroundService : IDisposable
{
    bool IsEnabled { get; set; }

    event Action<List<Color>>? BackgroundColorsChanged;

    Task InitializeAsync(FrameworkElement? targetElement = null);

    Task UpdateBackgroundAsync();
}
