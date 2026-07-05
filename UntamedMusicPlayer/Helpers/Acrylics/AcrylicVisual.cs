using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using UntamedMusicPlayer.Services;
using Windows.Foundation;

namespace UntamedMusicPlayer.Helpers.Acrylics;

public partial class AcrylicVisual : Control
{
    private static readonly SystemBackdropConfiguration _configuration = new()
    {
        IsInputActive = true,
    };

    private readonly ContentExternalBackdropLink _backdropLink;
    private readonly DesktopAcrylicController _controller;
    private Visual? _placementVisual;
    private RectangleClip? _clip;

    /// <summary>
    /// 剪裁偏移量（左、上、右、下偏移），用于调整剪裁区域。
    /// </summary>
    public Vector4 ClipOffset { get; set; } = Vector4.Zero;

    public AcrylicVisual()
    {
        // 监听 CornerRadius 属性变化
        RegisterPropertyChangedCallback(CornerRadiusProperty, OnCornerRadiusChanged);

        // 监听 RequestedTheme 变化
        RegisterPropertyChangedCallback(RequestedThemeProperty, OnRequestedThemeChanged);
        // 监听 ActualTheme 变化（系统主题变化时触发）
        ActualThemeChanged += OnActualThemeChanged;

        // 初始化 Composition 资源
        var compositor = CompositionTarget.GetCompositorForCurrentThread();
        _backdropLink = ContentExternalBackdropLink.Create(compositor);
        _backdropLink.ExternalBackdropBorderMode = CompositionBorderMode.Soft;
        _controller = new DesktopAcrylicController();
        UpdateTheme(ThemeSelectorService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light);

        // 在控件加载时更新可视树
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateVisual();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateVisual();
        if (_placementVisual is not null)
        {
            _placementVisual.Size = finalSize.ToVector2();
            if (_clip is not null)
            {
                _clip.Right = (float)(finalSize.Width + ClipOffset.Z);
                _clip.Bottom = (float)(finalSize.Height + ClipOffset.W);
            }
        }
        return finalSize;
    }

    private void OnCornerRadiusChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateVisual();
        var radius = CornerRadius;
        if (_clip is not null)
        {
            _clip.TopLeftRadius = new Vector2((float)radius.TopLeft, (float)radius.TopLeft);
            _clip.TopRightRadius = new Vector2((float)radius.TopRight, (float)radius.TopRight);
            _clip.BottomLeftRadius = new Vector2(
                (float)radius.BottomLeft,
                (float)radius.BottomLeft
            );
            _clip.BottomRightRadius = new Vector2(
                (float)radius.BottomRight,
                (float)radius.BottomRight
            );
        }
        else
        {
            var actualSize = ActualSize;
            var compositor = CompositionTarget.GetCompositorForCurrentThread();
            _clip = compositor.CreateRectangleClip(
                ClipOffset.X,
                ClipOffset.Y,
                (float)(actualSize.X + ClipOffset.Z),
                (float)(actualSize.Y + ClipOffset.W),
                new Vector2((float)radius.TopLeft, (float)radius.TopLeft),
                new Vector2((float)radius.TopRight, (float)radius.TopRight),
                new Vector2((float)radius.BottomRight, (float)radius.BottomRight),
                new Vector2((float)radius.BottomLeft, (float)radius.BottomLeft)
            );
            _placementVisual?.Clip = _clip;
        }
    }

    private void OnRequestedThemeChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateTheme(GetEffectiveTheme());
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateTheme(GetEffectiveTheme());
    }

    private void UpdateTheme(ElementTheme theme)
    {
        _configuration.Theme = ElementThemeToBackdropTheme(theme);
        _controller.SetSystemBackdropConfiguration(_configuration);
    }

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

    private ElementTheme GetEffectiveTheme()
    {
        // 优先使用 RequestedTheme，如果为 Default 则使用 ActualTheme
        var requested = RequestedTheme;
        if (requested != ElementTheme.Default)
        {
            return requested;
        }
        return ActualTheme;
    }

    private void UpdateVisual()
    {
        // 尝试从父级获取实际主题
        if (Parent is FrameworkElement parentElement)
        {
            _configuration.Theme = ElementThemeToBackdropTheme(parentElement.ActualTheme);
        }

        if (_placementVisual is null)
        {
            _controller.AddSystemBackdropTarget(_backdropLink);
            _controller.SetSystemBackdropConfiguration(_configuration);
            _placementVisual = _backdropLink.PlacementVisual;
            _placementVisual.BorderMode = CompositionBorderMode.Soft;
            ElementCompositionPreview.SetElementChildVisual(this, _placementVisual);
        }
    }
}
