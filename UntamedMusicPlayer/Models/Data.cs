using Microsoft.Extensions.DependencyInjection;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Core.Contracts.Services;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.Services;
using UntamedMusicPlayer.ViewModels;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Models;

public static class Data
{
    private static bool _isMusicProcessing;
    private static bool _isFileActivationLaunch;

    private static IAppStateService? GetAppStateService()
    {
        var app = App.Current as App;
        return app?.Host.Services.GetService<IAppStateService>();
    }

    /// <summary>
    /// 是否正在下载或更改音乐
    /// </summary>
    public static bool IsMusicProcessing
    {
        get => GetAppStateService()?.IsMusicProcessing ?? _isMusicProcessing;
        set
        {
            var appState = GetAppStateService();
            if (appState is not null)
            {
                appState.IsMusicProcessing = value;
                return;
            }

            _isMusicProcessing = value;
        }
    }

    /// <summary>
    /// 是否为文件激活启动（通过文件关联启动）
    /// </summary>
    public static bool IsFileActivationLaunch
    {
        get => GetAppStateService()?.IsFileActivationLaunch ?? _isFileActivationLaunch;
        set
        {
            var appState = GetAppStateService();
            if (appState is not null)
            {
                appState.IsFileActivationLaunch = value;
                return;
            }

            _isFileActivationLaunch = value;
        }
    }

    /// <summary>
    /// 软件显示名称
    /// </summary>
    public static readonly string AppDisplayName = "AppDisplayName".GetLocalized();

    public static PlayQueueManager PlayQueueManager { get; set; } = null!;
    public static LyricManager LyricManager { get; set; } = null!;
    public static SharedPlaybackState PlayState { get; set; } = null!;

    #region Views
    public static MainWindow? MainWindow { get; set; }
    public static ShellPage? ShellPage { get; set; }
    public static HomePage? HomePage { get; set; }
    public static LyricPage? LyricPage { get; set; }
    public static RootPlayBarView? RootPlayBarView { get; set; }
    public static DesktopLyricWindow? DesktopLyricWindow { get; set; }
    public static Dictionary<Guid, ImageViewerWindow>? ImageViewerWindows { get; set; }
    #endregion

    #region ViewModels
    public static SettingsViewModel? SettingsViewModel { get; set; }
    public static ShellViewModel? ShellViewModel { get; set; }
    public static RootPlayBarViewModel? RootPlayBarViewModel { get; set; }
    public static LocalSongsViewModel? LocalSongsViewModel { get; set; }
    public static LocalAlbumsViewModel? LocalAlbumsViewModel { get; set; }
    #endregion
}
