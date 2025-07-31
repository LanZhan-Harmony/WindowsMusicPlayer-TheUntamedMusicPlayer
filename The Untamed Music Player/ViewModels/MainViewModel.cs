using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using Windows.UI;
using WinRT;

namespace The_Untamed_Music_Player.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly ILocalSettingsService _localSettingsService =
        App.GetService<ILocalSettingsService>();
    private readonly MainWindow _mainMindow;
    private readonly ICompositionSupportsSystemBackdrop? _backdropTarget;
    private readonly SystemBackdropConfiguration _configurationSource = new()
    {
        IsInputActive = true,
    };
    private ISystemBackdropControllerWithTargets? _currentBackdropController;
    private bool _previousIsDarkTheme = false;

    public bool FirstStart { get; set; } = true;
    public byte SelectedMaterial { get; set; } = 3;
    public bool IsFallBack { get; set; } = true;
    public byte LuminosityOpacity { get; set; } = 85;
    public Color TintColor { get; set; } = default;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    [ObservableProperty]
    public partial double MainWindowWidth { get; set; }

    public MainViewModel()
    {
        _mainMindow = Data.MainWindow ?? new();
        _backdropTarget = _mainMindow.As<ICompositionSupportsSystemBackdrop>();
        MainWindowWidth = _mainMindow.Width;
        IsDarkTheme =
            ((FrameworkElement)_mainMindow.Content).ActualTheme == ElementTheme.Dark
            || (
                ((FrameworkElement)_mainMindow.Content).ActualTheme == ElementTheme.Default
                && App.Current.RequestedTheme == ApplicationTheme.Dark
            );
        InitializeAsync();
        _mainMindow.Activated += MainWindow_Activated;
        _mainMindow.Closed += MainWindow_Closed;
        _mainMindow.SizeChanged += MainMindow_SizeChanged;
        ((FrameworkElement)_mainMindow.Content).ActualThemeChanged += Window_ThemeChanged;
        Data.MainViewModel = this;
    }

    public async void InitializeAsync()
    {
        await LoadSettingsAsync();
        ChangeMaterial(SelectedMaterial);
        SaveIsDarkThemeAsync();
    }

    public async void ChangeMaterial(byte material)
    {
        _mainMindow.SystemBackdrop = null;
        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();

        _currentBackdropController = material switch
        {
            1 => new MicaController { Kind = MicaKind.Base },
            2 => new MicaController { Kind = MicaKind.BaseAlt },
            3 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Default },
            4 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Base },
            5 => new DesktopAcrylicController { Kind = DesktopAcrylicKind.Thin },
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
            _ = material switch
            {
                6 => TrySetBlurBackdrop(),
                7 => TrySetTransparentBackdrop(),
                8 => TrySetAnimatedBackdrop(),
                _ => false,
            };
        }

        if (FirstStart && IsDarkTheme == _previousIsDarkTheme)
        {
            ChangeLuminosityOpacity(LuminosityOpacity);
            ChangeTintColor(TintColor);
            FirstStart = false;
        }
        else
        {
            LuminosityOpacity = GetLuminosityOpacity();
            TintColor = GetTintColor();
            if (Data.SettingsViewModel is not null)
            {
                Data.SettingsViewModel.LuminosityOpacity = LuminosityOpacity;
                Data.SettingsViewModel.TintColor = TintColor;
            }
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

    public void ChangeLuminosityOpacity(byte luminosityOpacity)
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.LuminosityOpacity = luminosityOpacity / 100.0F;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.LuminosityOpacity = luminosityOpacity / 100.0F;
        }
    }

    public byte GetLuminosityOpacity()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            return (byte)(micaController.LuminosityOpacity * 100);
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            return (byte)(desktopAcrylicController.LuminosityOpacity * 100);
        }
        return LuminosityOpacity;
    }

    public void ChangeTintColor(Color tintColor)
    {
        if (_currentBackdropController is MicaController micaController)
        {
            micaController.TintColor = tintColor;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            desktopAcrylicController.TintColor = tintColor;
        }
    }

    public Color GetTintColor()
    {
        if (_currentBackdropController is MicaController micaController)
        {
            return micaController.TintColor;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            return desktopAcrylicController.TintColor;
        }
        return TintColor;
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        SetConfigurationSourceTheme();
        IsDarkTheme =
            ((FrameworkElement)_mainMindow.Content).ActualTheme == ElementTheme.Dark
            || (
                ((FrameworkElement)_mainMindow.Content).ActualTheme == ElementTheme.Default
                && App.Current.RequestedTheme == ApplicationTheme.Dark
            );
        ChangeTheme();
        SaveIsDarkThemeAsync();
    }

    private void ChangeTheme()
    {
        Color darkColor,
            lightColor;

        if (_currentBackdropController is MicaController micaController)
        {
            switch (SelectedMaterial)
            {
                case 1:
                    darkColor = Color.FromArgb(255, 32, 32, 32);
                    lightColor = Color.FromArgb(255, 243, 243, 243);
                    break;
                case 2:
                    darkColor = Color.FromArgb(255, 10, 10, 10);
                    lightColor = Color.FromArgb(255, 218, 218, 218);
                    break;
                default:
                    return;
            }
            var color = IsDarkTheme ? darkColor : lightColor;
            micaController.TintColor = color;
            TintColor = color;
            Data.SettingsViewModel?.TintColor = color;
        }
        else if (_currentBackdropController is DesktopAcrylicController desktopAcrylicController)
        {
            switch (SelectedMaterial)
            {
                case 3:
                    darkColor = Color.FromArgb(255, 44, 44, 44);
                    lightColor = Color.FromArgb(255, 252, 252, 252);
                    break;
                case 4:
                    darkColor = Color.FromArgb(255, 32, 32, 32);
                    lightColor = Color.FromArgb(255, 243, 243, 243);
                    break;
                case 5:
                    darkColor = Color.FromArgb(255, 84, 84, 84);
                    lightColor = Color.FromArgb(255, 211, 211, 211);
                    break;
                default:
                    return;
            }
            var color = IsDarkTheme ? darkColor : lightColor;
            desktopAcrylicController.TintColor = color;
            TintColor = color;
            Data.SettingsViewModel?.TintColor = color;
        }
    }

    private void SetConfigurationSourceTheme()
    {
        switch (((FrameworkElement)_mainMindow.Content).ActualTheme)
        {
            case ElementTheme.Dark:
                _configurationSource.Theme = SystemBackdropTheme.Dark;
                break;
            case ElementTheme.Light:
                _configurationSource.Theme = SystemBackdropTheme.Light;
                break;
            case ElementTheme.Default:
                _configurationSource.Theme = SystemBackdropTheme.Default;
                break;
        }
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

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Data.MusicPlayer.Stop();
        Data.MusicPlayer.PositionUpdateTimer250ms?.Cancel();
        Data.MusicPlayer.PositionUpdateTimer250ms = null;
        _mainMindow.SystemBackdrop = null;
        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();
        _mainMindow.Activated -= MainWindow_Activated;
        Data.DesktopLyricWindow?.Close();
        Data.DesktopLyricWindow?.Dispose();
        Data.MusicPlayer.Player.Dispose();
        Data.MusicPlayer.SaveCurrentStateAsync();
    }

    private void MainMindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        MainWindowWidth = args.Size.Width;
    }

    /// <summary>
    /// 从设置存储读取选定的材质
    /// </summary>
    /// <returns></returns>
    private async Task LoadSettingsAsync()
    {
        var fontName = await _localSettingsService.ReadSettingAsync<string>("SelectedFontFamily");
        var fontSize = await _localSettingsService.ReadSettingAsync<double>("SelectedFontSize");
        if (!string.IsNullOrEmpty(fontName))
        {
            Data.SelectedFontFamily = new FontFamily(fontName);
        }
        if (fontSize != 0.0)
        {
            Data.SelectedFontSize = fontSize;
        }
        Data.IsLyricBackgroundVisible = await _localSettingsService.ReadSettingAsync<bool>(
            "IsLyricBackgroundVisible"
        );
        Data.NotFirstUsed = await _localSettingsService.ReadSettingAsync<bool>("NotFirstUsed");
        if (Data.NotFirstUsed)
        {
            _previousIsDarkTheme = await _localSettingsService.ReadSettingAsync<bool>(
                "IsDarkTheme"
            );
            SelectedMaterial = await _localSettingsService.ReadSettingAsync<byte>(
                "SelectedMaterial"
            );
            IsFallBack = await _localSettingsService.ReadSettingAsync<bool>("IsFallBack");
            LuminosityOpacity = await _localSettingsService.ReadSettingAsync<byte>(
                "LuminosityOpacity"
            );
            TintColor = await _localSettingsService.ReadSettingAsync<Color>("TintColor");
        }
        else
        {
            var darkColor = Color.FromArgb(255, 44, 44, 44);
            var lightColor = Color.FromArgb(255, 252, 252, 252);
            TintColor = IsDarkTheme ? darkColor : lightColor;
            await _localSettingsService.SaveSettingAsync("NotFirstUsed", true);
            await _localSettingsService.SaveSettingAsync("SelectedMaterial", SelectedMaterial);
            await _localSettingsService.SaveSettingAsync("IsFallBack", IsFallBack);
            await _localSettingsService.SaveSettingAsync("LuminosityOpacity", LuminosityOpacity);
            await _localSettingsService.SaveSettingAsync("TintColor", TintColor);
        }
    }

    private async void SaveIsDarkThemeAsync()
    {
        await _localSettingsService.SaveSettingAsync("IsDarkTheme", IsDarkTheme);
    }
}
