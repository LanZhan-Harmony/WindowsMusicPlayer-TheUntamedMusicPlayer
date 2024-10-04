using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.UI;
using WinRT;

namespace The_Untamed_Music_Player.ViewModels;

public class MainViewModel
{
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();
    private readonly MainWindow _mainMindow;
    private readonly ICompositionSupportsSystemBackdrop? _backdropTarget;
    private ISystemBackdropControllerWithTargets? _currentBackdropController;

    private byte _selectedMaterial;
    /// <summary>
    /// 选定的窗口材质
    /// </summary>
    public byte SelectedMaterial
    {
        get => _selectedMaterial;
        set => _selectedMaterial = value;
    }

    private bool _isFallBack;
    public bool IsFallBack
    {
        get => _isFallBack;
        set => _isFallBack = value;
    }

    private byte _luminosityOpacity = 60;
    public byte LuminosityOpacity
    {
        get => _luminosityOpacity;
        set => _luminosityOpacity = value;
    }

    private int _tintOpacity = 60;
    public int TintOpacity
    {
        get => _tintOpacity;
        set => _tintOpacity = value;
    }

    private Color _tintColor;
    public Color TintColor
    {
        get => _tintColor;
        set => _tintColor = value;
    }

    public static readonly SystemBackdropConfiguration configuration = new()
    {
        IsInputActive = true,
    };

    public MainViewModel()
    {
        _mainMindow = Data.MainWindow ?? new();
        _backdropTarget = _mainMindow.As<ICompositionSupportsSystemBackdrop>();
        _mainMindow.Activated += MainWindow_Activated;
        _mainMindow.Closed += MainWindow_Closed;
    }

    public void ChangeMaterial(byte material)
    {
        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();

        _currentBackdropController = material switch
        {
            1 => new MicaController { Kind = MicaKind.Base },
            2 => new MicaController { Kind = MicaKind.BaseAlt },
            3 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Default },
            4 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Base },
            5 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Thin },
            _ => null
        };

        if (_currentBackdropController != null)
        {
            _currentBackdropController?.AddSystemBackdropTarget(_backdropTarget);
            _currentBackdropController?.SetSystemBackdropConfiguration(configuration);
        }
        else
        {
            _ = material switch
            {
                6 => TrySetBlurBackdrop(),
                7 => TrySetTransparentBackdrop(),
                8 => TrySetAnimatedBackdrop(),
                _ => false
            };
        }
    }

    /// <summary>
    /// 尝试设置模糊背景
    /// </summary>
    /// <returns></returns>
    public bool TrySetBlurBackdrop()
    {
        try
        {
            var blurBackdrop = new BlurredBackdrop();
            _mainMindow.SystemBackdrop = blurBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置透明背景
    /// </summary>
    /// <param name="tintColor"></param>
    /// <returns></returns>
    public bool TrySetTransparentBackdrop(Color? tintColor = null)
    {
        try
        {
            var color = tintColor ?? Colors.Transparent;
            var transparentBackdrop = new TransparentTintBackdrop(color);
            _mainMindow.SystemBackdrop = transparentBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置动画背景
    /// </summary>
    /// <returns></returns>
    public bool TrySetAnimatedBackdrop()
    {
        try
        {
            var animatedBackdrop = new ColorAnimatedBackdrop();
            _mainMindow.SystemBackdrop = animatedBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void ChangeLuminosityOpacity()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.LuminosityOpacity = LuminosityOpacity / 100.0F;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.LuminosityOpacity = LuminosityOpacity / 100.0F;
        }
    }

    public void GetLuminosityOpacity()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            LuminosityOpacity = (byte)(micaController.LuminosityOpacity * 100);
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            LuminosityOpacity = (byte)(desktopAcrylicController.LuminosityOpacity * 100);
        }
    }

    public void ChangeTintOpacity()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.TintOpacity = TintOpacity / 100.0F;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.TintOpacity = TintOpacity / 100.0F;
        }
    }

    public void GetTintOpacity()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            TintOpacity = (int)(micaController.TintOpacity * 100);
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            TintOpacity = (int)(desktopAcrylicController.TintOpacity * 100);
        }
    }

    public void ChangeTintColor()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.TintColor = TintColor;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.TintColor = TintColor;
        }
    }

    public void GetTintColor()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            TintColor = micaController.TintColor;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            TintColor = desktopAcrylicController.TintColor;
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (IsFallBack)
        {
            _currentBackdropController?.SetSystemBackdropConfiguration(new()
            {
                IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated
            });
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();
    }

    /// <summary>
    /// 从设置存储读取选定的材质
    /// </summary>
    /// <returns></returns>
    public async Task LoadSelectedMaterialAsync()
    {
        SelectedMaterial = await _localSettingsService.ReadSettingAsync<bool>("NotFirstUsed")
            ? await _localSettingsService.ReadSettingAsync<byte>("SelectedMaterial")
            : (byte)3;
    }
}
