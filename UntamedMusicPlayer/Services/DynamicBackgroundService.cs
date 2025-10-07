using System.ComponentModel;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using Windows.UI;
using ZLogger;

namespace UntamedMusicPlayer.Services;

/// <summary>
/// 动态背景服务，根据当前播放歌曲的封面颜色动态改变窗口背景
/// </summary>
public partial class DynamicBackgroundService(IColorExtractionService colorExtractionService)
    : IDynamicBackgroundService
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DynamicBackgroundService>();
    private Compositor? _compositor;
    private SpriteVisual? _backgroundVisual;
    private FrameworkElement? _targetElement;
    private CompositionLinearGradientBrush? _currentGradientBrush;

    public bool IsEnabled
    {
        get;
        set
        {
            field = value;
            Settings.IsWindowBackgroundFollowsCover = value;
            _ = UpdateBackgroundAsync();
            if (!value)
            {
                ClearBackground();
            }
        }
    }

    /// <summary>
    /// 背景更新事件
    /// </summary>
    public event Action<List<Color>>? BackgroundColorsChanged;

    /// <summary>
    /// 初始化动态背景服务
    /// </summary>
    /// <param name="targetElement">目标元素（通常是MainWindow的根容器）</param>
    public async Task InitializeAsync(FrameworkElement? targetElement = null)
    {
        IsEnabled = Settings.IsWindowBackgroundFollowsCover;
        _targetElement = targetElement ?? Data.MainWindow!.GetBackgroundGrid();
        _compositor = ElementCompositionPreview.GetElementVisual(_targetElement).Compositor;

        // 监听当前歌曲变化
        Data.PlayState.PropertyChanged += OnStateChanged;
        await UpdateBackgroundAsync();
    }

    /// <summary>
    /// 手动更新背景（从当前播放歌曲）
    /// </summary>
    public async Task UpdateBackgroundAsync()
    {
        if (!IsEnabled || Data.PlayState.CurrentSong is null)
        {
            return;
        }

        await UpdateBackgroundFromSongAsync(Data.PlayState.CurrentSong);
    }

    /// <summary>
    /// 从歌曲信息更新背景
    /// </summary>
    /// <param name="song">歌曲信息</param>
    public async Task UpdateBackgroundFromSongAsync(IDetailedSongInfoBase song)
    {
        if (!IsEnabled || _compositor is null || _targetElement is null)
        {
            return;
        }

        try
        {
            List<Color> colors = [];

            // 根据歌曲类型提取颜色
            if (song is DetailedLocalSongInfo localSong && localSong.CoverBuffer is not null)
            {
                colors = await colorExtractionService.ExtractColorsAsync(localSong.CoverBuffer);
            }
            else if (
                song is IDetailedOnlineSongInfo onlineSong
                && !string.IsNullOrEmpty(onlineSong.CoverPath)
            )
            {
                colors = await colorExtractionService.ExtractColorsAsync(onlineSong.CoverPath);
            }

            if (colors.Count > 0)
            {
                App.MainWindow!.DispatcherQueue.TryEnqueue(() =>
                {
                    ApplyGradientBackground(colors);
                    BackgroundColorsChanged?.Invoke(colors);
                });
            }
            else
            {
                App.MainWindow!.DispatcherQueue.TryEnqueue(ClearBackground);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"更新动态背景失败");
            App.MainWindow!.DispatcherQueue.TryEnqueue(ClearBackground);
        }
    }

    /// <summary>
    /// 应用渐变背景
    /// </summary>
    /// <param name="colors">颜色列表</param>
    private void ApplyGradientBackground(List<Color> colors)
    {
        if (_compositor is null || _targetElement is null)
        {
            return;
        }

        try
        {
            // 创建渐变配置
            var gradientConfig = colorExtractionService.GenerateGradient(colors);

            // 创建线性渐变画刷
            _currentGradientBrush?.Dispose();
            _currentGradientBrush = _compositor.CreateLinearGradientBrush();

            // 设置渐变方向（-45度）
            var angle = gradientConfig.Angle * Math.PI / 180;
            _currentGradientBrush.StartPoint = new Vector2(0, 0);
            _currentGradientBrush.EndPoint = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            );

            // 添加颜色停止点
            var gradientColors = gradientConfig.Colors;
            for (var i = 0; i < gradientColors.Count; i++)
            {
                var position = (float)i / (gradientColors.Count - 1);
                var color = AdjustColorForBackground(gradientColors[i]);
                _currentGradientBrush.ColorStops.Add(
                    _compositor.CreateColorGradientStop(position, color)
                );
            }

            // 创建或更新背景视觉元素
            if (_backgroundVisual is null)
            {
                _backgroundVisual = _compositor.CreateSpriteVisual();
                ElementCompositionPreview.SetElementChildVisual(_targetElement, _backgroundVisual);
            }

            _backgroundVisual.Brush = _currentGradientBrush;
            _backgroundVisual.Size = new Vector2(
                (float)_targetElement.ActualWidth,
                (float)_targetElement.ActualHeight
            );
            _backgroundVisual.Opacity = 0.5f; // 提高到50%透明度，增强视觉效果

            // 添加淡入动画
            var fadeInAnimation = _compositor.CreateScalarKeyFrameAnimation();
            fadeInAnimation.InsertKeyFrame(0f, 0f);
            fadeInAnimation.InsertKeyFrame(1f, 0.5f); // 与上面的透明度保持一致
            fadeInAnimation.Duration = TimeSpan.FromMilliseconds(800);
            fadeInAnimation.Target = "Opacity";

            _backgroundVisual.StartAnimation("Opacity", fadeInAnimation);

            // 监听窗口大小变化
            _targetElement.SizeChanged -= OnTargetElementSizeChanged;
            _targetElement.SizeChanged += OnTargetElementSizeChanged;
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"应用渐变背景失败");
        }
    }

    /// <summary>
    /// 调整颜色用于背景显示
    /// </summary>
    /// <param name="color">原始颜色</param>
    /// <returns>调整后的颜色</returns>
    private static Color AdjustColorForBackground(Color color)
    {
        // 调整颜色参数，保持更多的饱和度和亮度
        var hsv = ColorToHsv(color);
        hsv.Saturation *= 1f; // 保留100%的饱和度
        hsv.Value = Math.Max(0.5f, Math.Min(0.9f, hsv.Value * 0.8f)); // 亮度范围50%-90%

        return HsvToColor(hsv);
    }

    /// <summary>
    /// 清除背景
    /// </summary>
    private void ClearBackground()
    {
        if (_backgroundVisual is not null)
        {
            var fadeOutAnimation = _compositor?.CreateScalarKeyFrameAnimation();
            if (fadeOutAnimation is not null)
            {
                fadeOutAnimation.InsertKeyFrame(0f, _backgroundVisual.Opacity);
                fadeOutAnimation.InsertKeyFrame(1f, 0f);
                fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(400);

                var batch = _compositor!.CreateScopedBatch(CompositionBatchTypes.Animation);
                batch.Completed += (s, e) =>
                {
                    _backgroundVisual.Brush = null;
                };

                _backgroundVisual.StartAnimation("Opacity", fadeOutAnimation);
                batch.End();
            }
            else
            {
                _backgroundVisual.Brush = null;
            }
        }

        _currentGradientBrush?.Dispose();
        _currentGradientBrush = null;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SharedPlaybackState.CurrentSong))
        {
            _ = Task.Run(UpdateBackgroundAsync);
        }
    }

    private void OnTargetElementSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _backgroundVisual?.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
    }

    private static (float Hue, float Saturation, float Value) ColorToHsv(Color color)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;

        var max = Math.Max(Math.Max(r, g), b);
        var min = Math.Min(Math.Min(r, g), b);
        var delta = max - min;

        var hue = 0f;
        if (delta != 0)
        {
            if (max == r)
            {
                hue = (g - b) / delta % 6;
            }
            else if (max == g)
            {
                hue = (b - r) / delta + 2;
            }
            else
            {
                hue = (r - g) / delta + 4;
            }

            hue *= 60;
            if (hue < 0)
            {
                hue += 360;
            }
        }

        var saturation = max == 0 ? 0 : delta / max;
        var value = max;

        return (hue, saturation, value);
    }

    private static Color HsvToColor((float Hue, float Saturation, float Value) hsv)
    {
        var c = hsv.Value * hsv.Saturation;
        var x = c * (1 - Math.Abs((hsv.Hue / 60) % 2 - 1));
        var m = hsv.Value - c;

        float r,
            g,
            b;

        if (hsv.Hue >= 0 && hsv.Hue < 60)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (hsv.Hue >= 60 && hsv.Hue < 120)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (hsv.Hue >= 120 && hsv.Hue < 180)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (hsv.Hue >= 180 && hsv.Hue < 240)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (hsv.Hue >= 240 && hsv.Hue < 300)
        {
            r = x;
            g = 0;
            b = c;
        }
        else
        {
            r = c;
            g = 0;
            b = x;
        }

        return Color.FromArgb(
            255,
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        Data.PlayState.PropertyChanged -= OnStateChanged;
        _backgroundVisual?.Dispose();
        _currentGradientBrush?.Dispose();
        _targetElement?.SizeChanged -= OnTargetElementSizeChanged;
        GC.SuppressFinalize(this);
    }
}
