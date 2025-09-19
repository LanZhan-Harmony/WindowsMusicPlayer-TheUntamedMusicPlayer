using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.UI;
using WinRT;
using WinUIEx;

namespace The_Untamed_Music_Player.Services;

public partial class MaterialSelectorService : IMaterialSelectorService
{
    private WindowEx _mainWindow = null!;
    private ICompositionSupportsSystemBackdrop? _backdropTarget;
    private readonly SystemBackdropConfiguration _configurationSource = new()
    {
        IsInputActive = true,
    };
    private ISystemBackdropControllerWithTargets? _currentBackdropController;

    // 防抖相关字段
    private Timer? _debounceTimer;
    private readonly Lock _debounceLock = new();
    private const int DEBOUNCE_DELAY_MS = 100; // 100毫秒防抖延迟

    public MaterialType Material
    {
        get;
        set
        {
            field = value;
            Settings.Material = value;
        }
    }
    public bool IsFallBack
    {
        get;
        set
        {
            field = value;
            Settings.IsFallBack = value;
        }
    }
    public byte LuminosityOpacity
    {
        get;
        set
        {
            field = value;
            Settings.LuminosityOpacity = value;
        }
    }
    public Color TintColor
    {
        get;
        set
        {
            field = value;
            Settings.TintColor = value;
        }
    }

    public void InitializeSettings()
    {
        Material = Settings.Material;
        IsFallBack = Settings.IsFallBack;
        LuminosityOpacity = Settings.LuminosityOpacity;
        TintColor = Settings.TintColor;
    }

    public async Task InitializeMaterialAsync()
    {
        _mainWindow = App.MainWindow!;
        _mainWindow.Activated += MainWindow_Activated;
        ((FrameworkElement)_mainWindow.Content).ActualThemeChanged += Window_ThemeChanged;
        _backdropTarget = _mainWindow.As<ICompositionSupportsSystemBackdrop>();
        await SetMaterial(Material, true, true);
    }

    public async Task<(byte, Color)> SetMaterial(
        MaterialType material,
        bool firstStart = false,
        bool forced = false
    )
    {
        try
        {
            if ((Material == material && !forced) || _mainWindow is null)
            {
                return (LuminosityOpacity, TintColor);
            }
            _mainWindow.SystemBackdrop = null;
            _currentBackdropController?.RemoveAllSystemBackdropTargets();
            _currentBackdropController?.Dispose();
            _currentBackdropController = material switch
            {
                MaterialType.Mica => new MicaController { Kind = MicaKind.Base },
                MaterialType.MicaAlt => new MicaController { Kind = MicaKind.BaseAlt },
                MaterialType.DesktopAcrylic => new DesktopAcrylicController
                {
                    Kind = DesktopAcrylicKind.Default,
                },
                MaterialType.AcrylicBase => new DesktopAcrylicController
                {
                    Kind = DesktopAcrylicKind.Base,
                },
                MaterialType.AcrylicThin => new DesktopAcrylicController
                {
                    Kind = DesktopAcrylicKind.Thin,
                },
                _ => null,
            };
            if (_currentBackdropController is not null)
            {
                SetConfigurationSourceTheme();
                _currentBackdropController?.AddSystemBackdropTarget(_backdropTarget);
                _currentBackdropController?.SetSystemBackdropConfiguration(_configurationSource);
                await Task.Delay(100);
            }
            else
            {
                _mainWindow.SystemBackdrop = material switch
                {
                    MaterialType.Blur => new BlurredBackdrop(),
                    MaterialType.Transparent => new TransparentTintBackdrop(),
                    MaterialType.Animated => new ColorAnimatedBackdrop(),
                    _ => null,
                };
            }

            if (firstStart && ThemeSelectorService.IsDarkTheme == Settings.PreviousIsDarkTheme)
            {
                SetLuminosityOpacity(LuminosityOpacity, true);
                SetTintColor(TintColor, true);
            }
            else
            {
                LuminosityOpacity = GetLuminosityOpacity();
                TintColor = GetTintColor();
            }
        }
        catch { }
        Material = material;
        return (LuminosityOpacity, TintColor);
    }

    public void SetLuminosityOpacity(byte opacity, bool forced = false)
    {
        if (LuminosityOpacity == opacity && !forced)
        {
            return;
        }

        lock (_debounceLock)
        {
            _debounceTimer?.Dispose(); // 取消之前的定时器
            _debounceTimer = new Timer( // 创建新的定时器，延迟执行
                _ =>
                {
                    try
                    {
                        if (_currentBackdropController is MicaController micaController)
                        {
                            micaController.LuminosityOpacity = opacity / 100f;
                        }
                        else if (
                            _currentBackdropController
                            is DesktopAcrylicController desktopAcrylicController
                        )
                        {
                            desktopAcrylicController.LuminosityOpacity = opacity / 100f;
                        }
                    }
                    catch { }
                    finally
                    {
                        lock (_debounceLock)
                        {
                            _debounceTimer?.Dispose();
                            _debounceTimer = null;
                        }
                    }
                },
                null,
                DEBOUNCE_DELAY_MS,
                Timeout.Infinite
            );
        }
        LuminosityOpacity = opacity;
    }

    public void SetTintColor(Color color, bool forced = false)
    {
        if (TintColor == color && !forced)
        {
            return;
        }

        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                _ =>
                {
                    try
                    {
                        if (_currentBackdropController is MicaController micaController)
                        {
                            micaController.TintColor = color;
                        }
                        else if (
                            _currentBackdropController
                            is DesktopAcrylicController desktopAcrylicController
                        )
                        {
                            desktopAcrylicController.TintColor = color;
                        }
                    }
                    catch { }
                    finally
                    {
                        lock (_debounceLock)
                        {
                            _debounceTimer?.Dispose();
                            _debounceTimer = null;
                        }
                    }
                },
                null,
                DEBOUNCE_DELAY_MS,
                Timeout.Infinite
            );
        }
        TintColor = color;
    }

    private void SetConfigurationSourceTheme()
    {
        _configurationSource.Theme = ((FrameworkElement)_mainWindow.Content).ActualTheme switch
        {
            ElementTheme.Default => SystemBackdropTheme.Default,
            ElementTheme.Light => SystemBackdropTheme.Light,
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            _ => SystemBackdropTheme.Default,
        };
        Settings.PreviousIsDarkTheme = ThemeSelectorService.IsDarkTheme;
    }

    /// <summary>
    /// 获取不透明度
    /// </summary>
    /// <returns></returns>
    public byte GetLuminosityOpacity()
    {
        return _currentBackdropController switch
        {
            MicaController micaController => (byte)(micaController.LuminosityOpacity * 100),
            DesktopAcrylicController desktopAcrylicController => (byte)(
                desktopAcrylicController.LuminosityOpacity * 100
            ),
            _ => LuminosityOpacity,
        };
    }

    /// <summary>
    /// 获取背景颜色
    /// </summary>
    /// <returns></returns>
    public Color GetTintColor()
    {
        return _currentBackdropController switch
        {
            MicaController micaController => micaController.TintColor,
            DesktopAcrylicController desktopAcrylicController => desktopAcrylicController.TintColor,
            _ => TintColor,
        };
    }

    private void ChangeTheme()
    {
        Color color;
        if (ThemeSelectorService.IsDarkTheme)
        {
            color = Material switch
            {
                MaterialType.Mica => ColorFromHex(0xFF202020),
                MaterialType.MicaAlt => ColorFromHex(0xFF0A0A0A),
                MaterialType.DesktopAcrylic => ColorFromHex(0xFF2C2C2C),
                MaterialType.AcrylicBase => ColorFromHex(0xFF202020),
                MaterialType.AcrylicThin => ColorFromHex(0xFF545454),
                _ => TintColor,
            };
        }
        else
        {
            color = Material switch
            {
                MaterialType.Mica => ColorFromHex(0xFFF3F3F3),
                MaterialType.MicaAlt => ColorFromHex(0xFFDADADA),
                MaterialType.DesktopAcrylic => ColorFromHex(0xFFFCFCFC),
                MaterialType.AcrylicBase => ColorFromHex(0xFFF3F3F3),
                MaterialType.AcrylicThin => ColorFromHex(0xFFD3D3D3),
                _ => TintColor,
            };
        }
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.TintColor = color;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.TintColor = color;
        }
        TintColor = color;
    }

    private static Color ColorFromHex(uint hex)
    {
        var a = (byte)((hex >> 24) & 0xFF);
        var r = (byte)((hex >> 16) & 0xFF);
        var g = (byte)((hex >> 8) & 0xFF);
        var b = (byte)(hex & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (IsFallBack)
        {
            _currentBackdropController?.SetSystemBackdropConfiguration(
                new()
                {
                    IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated,
                }
            );
        }
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        TitleBarHelper.UpdateTitleBar(sender.ActualTheme);
        SetConfigurationSourceTheme();
        ChangeTheme();
    }

    public void Dispose()
    {
        // 清理防抖定时器
        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();
        _currentBackdropController = null;
        _mainWindow.Activated -= MainWindow_Activated;
        ((FrameworkElement)_mainWindow.Content).ActualThemeChanged -= Window_ThemeChanged;
        GC.SuppressFinalize(this);
    }
}
