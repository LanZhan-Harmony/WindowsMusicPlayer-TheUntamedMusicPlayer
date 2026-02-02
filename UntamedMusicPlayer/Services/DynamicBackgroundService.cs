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
public sealed partial class DynamicBackgroundService(IColorExtractionService colorExtractionService)
    : IDynamicBackgroundService
{
    private readonly ILogger _logger = LoggingService.CreateLogger<DynamicBackgroundService>();
    private Compositor? _compositor;
    private SpriteVisual? _backgroundVisual1;
    private SpriteVisual? _backgroundVisual2;
    private ContainerVisual? _containerVisual;
    private FrameworkElement? _targetElement;
    private CompositionLinearGradientBrush? _gradientBrush1;
    private CompositionLinearGradientBrush? _gradientBrush2;
    private bool _useFirstVisual = true;

    public bool IsEnabled
    {
        get => Settings.IsWindowBackgroundFollowsCover;
        set
        {
            if (Settings.IsWindowBackgroundFollowsCover == value)
            {
                return;
            }
            Settings.IsWindowBackgroundFollowsCover = value;
            if (value)
            {
                _ = UpdateBackgroundAsync();
            }
            else
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

            // 初始化容器（仅第一次）
            if (_containerVisual is null)
            {
                _containerVisual = _compositor.CreateContainerVisual();
                ElementCompositionPreview.SetElementChildVisual(_targetElement, _containerVisual);

                // 创建两个视觉层用于交叉淡入淡出
                _backgroundVisual1 = _compositor.CreateSpriteVisual();
                _backgroundVisual2 = _compositor.CreateSpriteVisual();

                _backgroundVisual1.Size = new Vector2(
                    (float)_targetElement.ActualWidth,
                    (float)_targetElement.ActualHeight
                );
                _backgroundVisual2.Size = new Vector2(
                    (float)_targetElement.ActualWidth,
                    (float)_targetElement.ActualHeight
                );

                _backgroundVisual1.Opacity = 0f;
                _backgroundVisual2.Opacity = 0f;

                _containerVisual.Children.InsertAtTop(_backgroundVisual1);
                _containerVisual.Children.InsertAtTop(_backgroundVisual2);

                // 监听窗口大小变化
                _targetElement.SizeChanged -= OnTargetElementSizeChanged;
                _targetElement.SizeChanged += OnTargetElementSizeChanged;
            }

            // 选择要使用的视觉层和画刷
            var targetVisual = _useFirstVisual ? _backgroundVisual1 : _backgroundVisual2;
            var targetBrush = _useFirstVisual ? _gradientBrush1 : _gradientBrush2;
            var oldVisual = _useFirstVisual ? _backgroundVisual2 : _backgroundVisual1;

            // 创建新的渐变画刷
            targetBrush?.Dispose();
            targetBrush = _compositor.CreateLinearGradientBrush();

            if (_useFirstVisual)
            {
                _gradientBrush1 = targetBrush;
            }
            else
            {
                _gradientBrush2 = targetBrush;
            }

            // 设置渐变方向
            var angle = gradientConfig.Angle * Math.PI / 180;
            targetBrush.StartPoint = new Vector2(0, 0);
            targetBrush.EndPoint = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

            // 添加颜色停止点
            var gradientColors = gradientConfig.Colors;
            for (var i = 0; i < gradientColors.Count; i++)
            {
                var position = (float)i / (gradientColors.Count - 1);
                var color = AdjustColorForBackground(gradientColors[i]);
                targetBrush.ColorStops.Add(_compositor.CreateColorGradientStop(position, color));
            }

            // 应用画刷到目标视觉层
            targetVisual!.Brush = targetBrush;

            // 创建交叉淡入淡出动画
            var fadeInAnimation = _compositor.CreateScalarKeyFrameAnimation();
            fadeInAnimation.InsertKeyFrame(0f, 0f);
            fadeInAnimation.InsertKeyFrame(1f, 0.5f);
            fadeInAnimation.Duration = TimeSpan.FromMilliseconds(800);

            var fadeOutAnimation = _compositor.CreateScalarKeyFrameAnimation();
            fadeOutAnimation.InsertKeyFrame(0f, oldVisual!.Opacity);
            fadeOutAnimation.InsertKeyFrame(1f, 0f);
            fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(800);

            // 同时播放淡入和淡出动画
            targetVisual.StartAnimation("Opacity", fadeInAnimation);
            oldVisual.StartAnimation("Opacity", fadeOutAnimation);

            // 切换视觉层标志
            _useFirstVisual = !_useFirstVisual;
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
        if (_backgroundVisual1 is not null && _compositor is not null)
        {
            var fadeOutAnimation1 = _compositor.CreateScalarKeyFrameAnimation();
            fadeOutAnimation1.InsertKeyFrame(0f, _backgroundVisual1.Opacity);
            fadeOutAnimation1.InsertKeyFrame(1f, 0f);
            fadeOutAnimation1.Duration = TimeSpan.FromMilliseconds(400);
            _backgroundVisual1.StartAnimation("Opacity", fadeOutAnimation1);
        }

        if (_backgroundVisual2 is not null && _compositor is not null)
        {
            var fadeOutAnimation2 = _compositor.CreateScalarKeyFrameAnimation();
            fadeOutAnimation2.InsertKeyFrame(0f, _backgroundVisual2.Opacity);
            fadeOutAnimation2.InsertKeyFrame(1f, 0f);
            fadeOutAnimation2.Duration = TimeSpan.FromMilliseconds(400);

            var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += (s, e) =>
            {
                _backgroundVisual1?.Brush = null;
                _backgroundVisual2?.Brush = null;
            };

            _backgroundVisual2.StartAnimation("Opacity", fadeOutAnimation2);
            batch.End();
        }

        _gradientBrush1?.Dispose();
        _gradientBrush1 = null;
        _gradientBrush2?.Dispose();
        _gradientBrush2 = null;
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
        var newSize = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        _backgroundVisual1?.Size = newSize;
        _backgroundVisual2?.Size = newSize;
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
        _targetElement?.SizeChanged -= OnTargetElementSizeChanged;

        _backgroundVisual1?.Dispose();
        _backgroundVisual2?.Dispose();
        _containerVisual?.Dispose();
        _gradientBrush1?.Dispose();
        _gradientBrush2?.Dispose();

        GC.SuppressFinalize(this);
    }
}
