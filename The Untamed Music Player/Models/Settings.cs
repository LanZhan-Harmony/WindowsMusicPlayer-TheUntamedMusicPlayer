using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using Windows.UI;

namespace The_Untamed_Music_Player.Models;

public static class Settings
{
    private static readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();

    #region 设置属性
    /// <summary>
    /// 是否是第一次使用本软件
    /// </summary>
    public static bool NotFirstUsed
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(NotFirstUsed), true);
            }
        }
    }

    /// <summary>
    /// 是否为独占模式
    /// </summary>
    public static bool IsExclusiveMode
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(IsExclusiveMode), value);
            }
        }
    }

    /// <summary>
    /// 仅添加歌曲所在文件夹
    /// </summary>
    public static bool IsOnlyAddSpecificFolder
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(IsOnlyAddSpecificFolder), value);
            }
        }
    }

    /// <summary>
    /// 歌词字体
    /// </summary>
    public static FontFamily FontFamily
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(FontFamily), value.Source);
            }
        }
    } = null!;

    /// <summary>
    /// 歌词字号
    /// </summary>
    public static double LyricPageCurrentFontSize
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                LyricPageNotCurrentFontSize = value * 0.4;
                _localSettingsService.SaveSettingAsync(nameof(LyricPageCurrentFontSize), value);
            }
        }
    }
    public static double LyricPageNotCurrentFontSize { get; set; }

    /// <summary>
    /// 应用主题
    /// </summary>
    public static ElementTheme Theme
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(Theme), $"{value}");
            }
        }
    }

    public static bool PreviousIsDarkTheme
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(PreviousIsDarkTheme), value);
            }
        }
    }

    /// <summary>
    /// 窗口材质
    /// </summary>
    public static MaterialType Material
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(Material), $"{value}");
            }
        }
    }

    /// <summary>
    /// 窗口失焦背景停止更新
    /// </summary>
    public static bool IsFallBack
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(IsFallBack), value);
            }
        }
    }

    /// <summary>
    /// 不透明度
    /// </summary>
    public static byte LuminosityOpacity
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(LuminosityOpacity), value);
            }
        }
    }

    /// <summary>
    /// 窗口背景颜色
    /// </summary>
    public static Color TintColor
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(TintColor), value);
            }
        }
    }

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    public static bool IsWindowBackgroundFollowsCover
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(
                    nameof(IsWindowBackgroundFollowsCover),
                    value
                );
            }
        }
    }
    #endregion

    /// <summary>
    /// 是否启用均衡器
    /// </summary>
    public static bool IsEqualizerOn
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(IsEqualizerOn), value);
            }
        }
    }

    /// <summary>
    /// 使用一起移动附近的滑块
    /// </summary>
    public static bool IsMoveNearby
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _localSettingsService.SaveSettingAsync(nameof(IsMoveNearby), value);
            }
        }
    }

    public static async Task InitializeAsync()
    {
        try
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
            var themeName = await _localSettingsService.ReadSettingAsync<string>(nameof(Theme));
            Theme = Enum.TryParse<ElementTheme>(themeName, out var cacheTheme)
                ? cacheTheme
                : ElementTheme.Default;
            var materialName = await _localSettingsService.ReadSettingAsync<string>(
                nameof(Material)
            );
            Material = Enum.TryParse<MaterialType>(materialName, out var cacheMaterial)
                ? cacheMaterial
                : MaterialType.DesktopAcrylic;
            IsWindowBackgroundFollowsCover = await _localSettingsService.ReadSettingAsync<bool>(
                nameof(IsWindowBackgroundFollowsCover)
            );

            if (NotFirstUsed)
            {
                var lyricPageCurrentFontSize = await _localSettingsService.ReadSettingAsync<double>(
                    nameof(LyricPageCurrentFontSize)
                );
                LyricPageCurrentFontSize =
                    lyricPageCurrentFontSize == 0 ? 50 : lyricPageCurrentFontSize;
                PreviousIsDarkTheme = await _localSettingsService.ReadSettingAsync<bool>(
                    nameof(PreviousIsDarkTheme)
                );
                IsFallBack = await _localSettingsService.ReadSettingAsync<bool>(nameof(IsFallBack));
                LuminosityOpacity = await _localSettingsService.ReadSettingAsync<byte>(
                    nameof(LuminosityOpacity)
                );
                TintColor = await _localSettingsService.ReadSettingAsync<Color>(nameof(TintColor));
            }
            else
            {
                NotFirstUsed = true;
                LyricPageCurrentFontSize = 50;
                IsFallBack = true;
                LuminosityOpacity = 85;
                var darkColor = Color.FromArgb(255, 44, 44, 44);
                var lightColor = Color.FromArgb(255, 252, 252, 252);
                TintColor =
                    App.Current.RequestedTheme == ApplicationTheme.Dark ? darkColor : lightColor;
            }
            InitializedLaterAsync();
        }
        catch { }
    }

    public static void InitializedLaterAsync()
    {
        _ = Task.Run(async () =>
        {
            IsEqualizerOn = await _localSettingsService.ReadSettingAsync<bool>(
                nameof(IsEqualizerOn)
            );
            IsMoveNearby = await _localSettingsService.ReadSettingAsync<bool>(nameof(IsMoveNearby));
        });
    }
}
