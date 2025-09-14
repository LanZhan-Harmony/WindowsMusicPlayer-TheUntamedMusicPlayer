using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using Windows.UI;

namespace The_Untamed_Music_Player.Models;

public static class Settings
{
    private static readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    /// <summary>
    /// 是否是第一次使用本软件
    /// </summary>
    public static bool NotFirstUsed { get; set; }

    /// <summary>
    /// 是否为独占模式
    /// </summary>
    public static bool IsExclusiveMode { get; set; }

    /// <summary>
    /// 仅添加歌曲所在文件夹
    /// </summary>
    public static bool IsOnlyAddSpecificFolder { get; set; }

    /// <summary>
    /// 歌词字体
    /// </summary>
    public static FontFamily FontFamily { get; set; } = null!;

    /// <summary>
    /// 歌词字号
    /// </summary>
    public static double LyricPageCurrentFontSize { get; set; } = 50.0;
    public static double LyricPageNotCurrentFontSize { get; set; } = 20.0;

    /// <summary>
    /// 应用主题
    /// </summary>
    public static ElementTheme Theme { get; set; }

    /// <summary>
    /// 窗口材质
    /// </summary>
    public static MaterialType Material { get; set; } = MaterialType.DesktopAcrylic;

    /// <summary>
    /// 窗口失焦背景停止更新
    /// </summary>
    public static bool IsFallBack { get; set; } = true;

    /// <summary>
    /// 不透明度
    /// </summary>
    public static byte LuminosityOpacity { get; set; } = 85;

    /// <summary>
    /// 窗口背景颜色
    /// </summary>
    public static Color TintColor { get; set; } = default;

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    public static bool IsWindowBackgroundFollowsCover { get; set; }

    public static async Task InitializeAsync()
    {
        NotFirstUsed = await _localSettingsService.ReadSettingAsync<bool>(nameof(NotFirstUsed));
        IsExclusiveMode = await _localSettingsService.ReadSettingAsync<bool>(
            nameof(IsExclusiveMode)
        );
        IsOnlyAddSpecificFolder = await _localSettingsService.ReadSettingAsync<bool>(
            nameof(IsOnlyAddSpecificFolder)
        );
        FontFamily = new(
            await _localSettingsService.ReadSettingAsync<string>(nameof(FontFamily))
                ?? "Microsoft YaHei"
        );
        var lyricPageCurrentFontSize = await _localSettingsService.ReadSettingAsync<double>(
            nameof(LyricPageCurrentFontSize)
        );
        if (lyricPageCurrentFontSize != 0)
        {
            LyricPageCurrentFontSize = lyricPageCurrentFontSize;
            LyricPageNotCurrentFontSize = lyricPageCurrentFontSize * 0.4;
        }
    }
}
