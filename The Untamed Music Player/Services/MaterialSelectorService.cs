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

public class MaterialSelectorService : IMaterialSelectorService
{
    private WindowEx _mainWindow = null!;
    private ICompositionSupportsSystemBackdrop? _backdropTarget;
    private readonly SystemBackdropConfiguration _configurationSource = new()
    {
        IsInputActive = true,
    };
    private ISystemBackdropControllerWithTargets? _currentBackdropController;
    public bool _firstStart = true;
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

    public async Task InitializeAsync()
    {
        _mainWindow = App.MainWindow!;
        _mainWindow.Activated += MainWindow_Activated;
        ((FrameworkElement)_mainWindow.Content).ActualThemeChanged += Window_ThemeChanged;
        _backdropTarget = _mainWindow.As<ICompositionSupportsSystemBackdrop>();
        Material = Settings.Material;
        IsFallBack = Settings.IsFallBack;
        LuminosityOpacity = Settings.LuminosityOpacity;
        TintColor = Settings.TintColor;
        await SetMaterial(Material);
    }

    public async Task<(byte, Color)> SetMaterial(MaterialType material)
    {
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

        if (_firstStart && ThemeSelectorService.IsDarkTheme == Settings.PreviousIsDarkTheme)
        {
            SetLuminosityOpacity(LuminosityOpacity);
            SetTintColor(TintColor);
            _firstStart = false;
        }
        else
        {
            LuminosityOpacity = GetLuminosityOpacity();
            TintColor = GetTintColor();
        }
        Material = material;
        return (LuminosityOpacity, TintColor);
    }

    public void SetIsFallBack(bool isFallBack) => IsFallBack = isFallBack;

    public void SetLuminosityOpacity(byte opacity)
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.LuminosityOpacity = opacity / 100f;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.LuminosityOpacity = opacity / 100f;
        }
        LuminosityOpacity = opacity;
    }

    public void SetTintColor(Color color)
    {
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
                MaterialType.Mica => Color.FromArgb(255, 32, 32, 32),
                MaterialType.MicaAlt => Color.FromArgb(255, 10, 10, 10),
                MaterialType.DesktopAcrylic => Color.FromArgb(255, 44, 44, 44),
                MaterialType.AcrylicBase => Color.FromArgb(255, 32, 32, 32),
                MaterialType.AcrylicThin => Color.FromArgb(255, 84, 84, 84),
                _ => TintColor,
            };
        }
        else
        {
            color = Material switch
            {
                MaterialType.Mica => Color.FromArgb(255, 243, 243, 243),
                MaterialType.MicaAlt => Color.FromArgb(255, 218, 218, 218),
                MaterialType.DesktopAcrylic => Color.FromArgb(255, 252, 252, 252),
                MaterialType.AcrylicBase => Color.FromArgb(255, 243, 243, 243),
                MaterialType.AcrylicThin => Color.FromArgb(255, 211, 211, 211),
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
        SetConfigurationSourceTheme();
        ChangeTheme();
    }
}
