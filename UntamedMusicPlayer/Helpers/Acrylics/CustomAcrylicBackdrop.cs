using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using UntamedMusicPlayer.Services;
using Windows.UI;

namespace UntamedMusicPlayer.Helpers.Acrylics;

/// <summary>
/// 自定义亚克力背景，支持设置 Kind、RequestedTheme 等属性，
/// 并允许动态调整颜色、透明度等。
/// </summary>
public partial class CustomAcrylicBackdrop : SystemBackdrop, IDisposable
{
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _configuration;

    // 依赖属性定义
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(DesktopAcrylicKind),
        typeof(CustomAcrylicBackdrop),
        new PropertyMetadata(DesktopAcrylicKind.Default, OnKindPropertyChanged)
    );

    public static readonly DependencyProperty RequestedThemeProperty = DependencyProperty.Register(
        nameof(RequestedTheme),
        typeof(ElementTheme),
        typeof(CustomAcrylicBackdrop),
        new PropertyMetadata(ElementTheme.Default, OnThemePropertyChanged)
    );

    // 基类中可能已定义以下属性，但为保险在此定义（若基类已有，可删除重复定义）
    // 注意：这些属性在 C++ 中来自 CustomBackdropBase，我们在此定义以便使用。
    public static readonly DependencyProperty FallbackColorProperty = DependencyProperty.Register(
        nameof(FallbackColor),
        typeof(Color),
        typeof(CustomAcrylicBackdrop),
        new PropertyMetadata(Colors.Transparent, OnFallbackColorChanged)
    );

    public static readonly DependencyProperty LuminosityOpacityProperty =
        DependencyProperty.Register(
            nameof(LuminosityOpacity),
            typeof(double),
            typeof(CustomAcrylicBackdrop),
            new PropertyMetadata(1.0, OnLuminosityOpacityChanged)
        );

    public static readonly DependencyProperty TintColorProperty = DependencyProperty.Register(
        nameof(TintColor),
        typeof(Color),
        typeof(CustomAcrylicBackdrop),
        new PropertyMetadata(Colors.Transparent, OnTintColorChanged)
    );

    public static readonly DependencyProperty TintOpacityProperty = DependencyProperty.Register(
        nameof(TintOpacity),
        typeof(double),
        typeof(CustomAcrylicBackdrop),
        new PropertyMetadata(1.0, OnTintOpacityChanged)
    );

    // CLR 属性包装
    public DesktopAcrylicKind Kind
    {
        get => (DesktopAcrylicKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public ElementTheme RequestedTheme
    {
        get => (ElementTheme)GetValue(RequestedThemeProperty);
        set => SetValue(RequestedThemeProperty, value);
    }

    public Color FallbackColor
    {
        get => (Color)GetValue(FallbackColorProperty);
        set => SetValue(FallbackColorProperty, value);
    }

    public double LuminosityOpacity
    {
        get => (double)GetValue(LuminosityOpacityProperty);
        set => SetValue(LuminosityOpacityProperty, value);
    }

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    // 静态属性变更回调
    private static void OnKindPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null)
        {
            self._controller.Kind = (DesktopAcrylicKind)e.NewValue;
        }
    }

    private static void OnThemePropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null && self._configuration is not null)
        {
            self._configuration.Theme = ElementThemeToBackdropTheme((ElementTheme)e.NewValue);
            self._controller.SetSystemBackdropConfiguration(self._configuration);
        }
    }

    private static void OnFallbackColorChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null)
        {
            self._controller.FallbackColor = (Color)e.NewValue;
        }
    }

    private static void OnLuminosityOpacityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null)
        {
            self._controller.LuminosityOpacity = (float)(double)e.NewValue;
        }
    }

    private static void OnTintColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null)
        {
            self._controller.TintColor = (Color)e.NewValue;
        }
    }

    private static void OnTintOpacityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var self = (CustomAcrylicBackdrop)d;
        if (self._controller is not null)
        {
            // WinUI 问题：必须先设置 TintColor 才能让 TintOpacity 生效
            self._controller.TintColor = Colors.Transparent; // 临时设为透明
            self._controller.TintOpacity = (float)(double)e.NewValue;
            self._controller.TintColor = self.TintColor; // 恢复原值
        }
    }

    // 辅助转换函数
    private static SystemBackdropTheme ElementThemeToBackdropTheme(ElementTheme theme)
    {
        return theme switch
        {
            ElementTheme.Light => SystemBackdropTheme.Light,
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            _ => ThemeSelectorService.IsDarkTheme
                ? SystemBackdropTheme.Dark
                : SystemBackdropTheme.Light,
        };
    }

    // 重写 SystemBackdrop 方法
    protected override void OnTargetConnected(
        ICompositionSupportsSystemBackdrop connectedTarget,
        XamlRoot xamlRoot
    )
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        // 创建配置对象（若尚未创建）
        _configuration ??= new SystemBackdropConfiguration { IsInputActive = true };

        // 应用 RequestedTheme（若已设置）
        var requestedTheme = (ElementTheme)GetValue(RequestedThemeProperty);
        if (requestedTheme != ElementTheme.Default)
        {
            _configuration.Theme = ElementThemeToBackdropTheme(requestedTheme);
        }

        // 创建控制器并应用所有属性
        MakeController(connectedTarget);
    }

    protected override void OnTargetDisconnected(
        ICompositionSupportsSystemBackdrop disconnectedTarget
    )
    {
        base.OnTargetDisconnected(disconnectedTarget);
        Dispose();
    }

    private void MakeController(ICompositionSupportsSystemBackdrop target)
    {
        _controller = new DesktopAcrylicController();

        // 设置配置
        _controller.AddSystemBackdropTarget(target);
        _controller.SetSystemBackdropConfiguration(_configuration);

        // 应用所有已设置的属性（读取本地值）
        ApplyLocalValue(FallbackColorProperty, (v) => _controller.FallbackColor = (Color)v);
        ApplyLocalValue(KindProperty, (v) => _controller.Kind = (DesktopAcrylicKind)v);
        ApplyLocalValue(
            LuminosityOpacityProperty,
            (v) => _controller.LuminosityOpacity = (float)(double)v
        );
        ApplyLocalValue(TintColorProperty, (v) => _controller.TintColor = (Color)v);
        ApplyLocalValue(TintOpacityProperty, (v) => _controller.TintOpacity = (float)(double)v);
    }

    private void ApplyLocalValue(DependencyProperty property, Action<object> applyAction)
    {
        if (ReadLocalValue(property) != DependencyProperty.UnsetValue)
        {
            applyAction(GetValue(property));
        }
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
        _configuration = null;
    }
}
