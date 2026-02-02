using System.ComponentModel;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using Windows.Foundation;
using Windows.UI;

namespace UntamedMusicPlayer.LyricRenderer;

/// <summary>
/// Apple Music 风格歌词渲染器
/// 使用 Win2D 进行高效渲染，支持模糊效果、字符级动画和弹簧滚动
/// </summary>
public sealed partial class AdvancedLyricRenderer : UserControl
{
    private readonly LyricRenderState _renderState = new();
    private readonly SharedPlaybackState _playState = Data.PlayState;
    private readonly LyricManager _lyricManager = Data.LyricManager;

    // 渲染参数
    private const float LineSpacing = 40f;
    private const float TranslationSpacing = 10f;
    private const float MinFontSize = 18f;
    private const float HoverBorderRadius = 12f;
    private const float HoverBorderPadding = 16f;

    // 缓存的测量信息
    private readonly Dictionary<int, LineMeasurement> _lineMeasurements = [];
    private float _totalContentHeight;
    private bool _measurementsDirty = true;

    // 事件
    public event EventHandler<LyricSlice>? LineClicked;

    /// <summary>
    /// 基础字体大小
    /// </summary>
    public float BaseFontSize { get; set; } = 48f;

    /// <summary>
    /// 翻译字体大小比例
    /// </summary>
    public float TranslationFontScale { get; set; } = 0.6f;

    /// <summary>
    /// 字体名称
    /// </summary>
    public string FontFamilyName { get; set; } = "Segoe UI";

    public AdvancedLyricRenderer()
    {
        InitializeComponent();
        LoadLyrics();
        UpdateSettings();
        _lyricManager.PropertyChanged += OnLyricPropertyChanged;
    }

    private void OnLyricPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LyricManager.CurrentLyricIndex))
        {
            // 当前行变化时，标记需要重新测量（因为字体大小会变化）
            _measurementsDirty = true;

            // 立即更新滚动目标，即使 measurements 还没更新
            // 这样可以避免延迟一帧
            if (!_renderState.IsUserInteracting && _renderState.Lines.Count > 0)
            {
                var targetScroll = CalculateScrollTargetForLine(_lyricManager.CurrentLyricIndex);
                _renderState.ScrollAnimator.SetTarget(targetScroll);
            }
        }
        else if (e.PropertyName == nameof(LyricManager.CurrentLyricSlices))
        {
            LoadLyrics();
        }
    }

    private void Canvas_CreateResources(
        CanvasAnimatedControl sender,
        CanvasCreateResourcesEventArgs args
    )
    {
        // 资源在需要时创建
    }

    private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
        var deltaTime = (float)args.Timing.ElapsedTime.TotalSeconds;

        // 检查用户交互超时
        if (_renderState.IsInteractionTimedOut)
        {
            _renderState.ResetInteraction();
        }

        // 更新滚动动画（目标已经在 OnLyricPropertyChanged 中设置）
        _renderState.ScrollAnimator.Update(deltaTime);
    }

    private void Canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
        if (_renderState.Lines.Count == 0)
        {
            return;
        }

        var ds = args.DrawingSession;
        var canvasSize = sender.Size;
        var canvasWidth = (float)canvasSize.Width;
        var canvasHeight = (float)canvasSize.Height;
        var centerY = canvasHeight / 2f;

        // 确保测量信息是最新的
        // 首次绘制或需要更新时都要重新测量
        if (_measurementsDirty || _lineMeasurements.Count == 0)
        {
            UpdateLineMeasurements(ds, canvasWidth);

            // 测量更新后，重新设置滚动目标以确保位置正确
            if (!_renderState.IsUserInteracting)
            {
                var targetScroll = CalculateScrollTargetForLine(_lyricManager.CurrentLyricIndex);
                _renderState.ScrollAnimator.SetTarget(targetScroll);
            }
        }

        // 当前滚动位置
        var scrollOffset = _renderState.ScrollAnimator.CurrentValue + _renderState.UserScrollOffset;

        // 渲染所有可见的歌词行
        var currentY = centerY - scrollOffset;

        for (var i = 0; i < _renderState.Lines.Count; i++)
        {
            var line = _renderState.Lines[i];
            var measurement = _lineMeasurements.GetValueOrDefault(i);
            var lineHeight = measurement.TotalHeight;

            // 跳过不可见的行
            if (currentY + lineHeight < 0 || currentY > canvasHeight)
            {
                currentY += lineHeight + LineSpacing;
                continue;
            }

            var distanceFromCenter = Math.Abs(i - _lyricManager.CurrentLyricIndex);
            var isCurrent = i == _lyricManager.CurrentLyricIndex;
            var isHovered = i == _renderState.HoveredLineIndex;

            // 计算样式参数
            var fontSize = BlurHelper.CalculateFontSize(
                distanceFromCenter,
                BaseFontSize,
                MinFontSize
            );
            var opacity = BlurHelper.CalculateOpacity(distanceFromCenter, isCurrent);
            var blurAmount = _renderState.IsUserInteracting
                ? 0
                : BlurHelper.CalculateBlurRadius(distanceFromCenter);

            // 渲染悬停高亮边框
            if (isHovered)
            {
                DrawHoverBackground(
                    ds,
                    0,
                    currentY - HoverBorderPadding,
                    canvasWidth,
                    lineHeight + HoverBorderPadding * 2
                );
            }

            // 渲染主歌词
            DrawMainLyric(
                ds,
                line,
                isCurrent,
                currentY,
                canvasWidth,
                fontSize,
                opacity,
                blurAmount
            );

            // 渲染翻译
            if (!string.IsNullOrEmpty(line.TranslationText))
            {
                var translationY = currentY + measurement.MainTextHeight + TranslationSpacing;
                var translationFontSize = fontSize * TranslationFontScale;
                DrawTranslation(
                    ds,
                    line.TranslationText,
                    translationY,
                    canvasWidth,
                    translationFontSize,
                    0.5f,
                    blurAmount
                );
            }

            currentY += lineHeight + LineSpacing;
        }
    }

    private void DrawMainLyric(
        CanvasDrawingSession ds,
        LyricLine line,
        bool isCurrent,
        float y,
        float canvasWidth,
        float fontSize,
        float opacity,
        int blurAmount
    )
    {
        using var format = new CanvasTextFormat
        {
            FontFamily = FontFamilyName,
            FontSize = fontSize,
            HorizontalAlignment =
                canvasWidth > 820
                    ? CanvasHorizontalAlignment.Left
                    : CanvasHorizontalAlignment.Center,
            WordWrapping = CanvasWordWrapping.Wrap,
        };

        var textWidth = canvasWidth - 40f; // 留边距
        var x = 20f;

        if (isCurrent && !_renderState.IsUserInteracting)
        {
            // 当前行使用字符级渐变效果
            DrawProgressiveText(ds, line, y, x, textWidth, format, opacity);
        }
        else
        {
            // 非当前行直接绘制
            var color = Color.FromArgb((byte)(opacity * 255), 255, 255, 255);
            if (blurAmount > 0)
            {
                // 模糊效果通过多次偏移绘制模拟
                DrawBlurredText(ds, line.MainText, x, y, textWidth, format, color, blurAmount);
            }
            else
            {
                using var layout = new CanvasTextLayout(
                    ds,
                    line.MainText,
                    format,
                    textWidth,
                    float.MaxValue
                );
                ds.DrawTextLayout(layout, x, y, color);
            }
        }
    }

    private void DrawProgressiveText(
        CanvasDrawingSession ds,
        LyricLine line,
        float y,
        float x,
        float textWidth,
        CanvasTextFormat format,
        float opacity
    )
    {
        var text = line.MainText;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        using var layout = new CanvasTextLayout(ds, text, format, textWidth, float.MaxValue);
        var progress = line.GetCharacterProgress(_playState.CurrentPlayingTime.TotalMilliseconds);

        // 如果没有进度信息，按全50%透明度绘制
        if (progress <= 0 || line.EndTime <= line.StartTime)
        {
            var staticColor = Color.FromArgb((byte)(opacity * 0.5f * 255), 255, 255, 255);
            ds.DrawTextLayout(layout, x, y, staticColor);
            return;
        }

        // 获取每个字符的边界
        var regions = layout.GetCharacterRegions(0, text.Length);

        for (var i = 0; i < text.Length && i < regions.Length; i++)
        {
            var region = regions[i];
            var charProgress = (float)i / text.Length;

            // 计算字符透明度
            float charOpacity;
            if (progress >= (charProgress + 1f / text.Length))
            {
                charOpacity = opacity; // 已完成，使用完整透明度
            }
            else if (progress > charProgress)
            {
                // 正在过渡：从50%到100%
                var t = (progress - charProgress) * text.Length;
                charOpacity = opacity * (0.5f + 0.5f * t);
            }
            else
            {
                charOpacity = opacity * 0.5f; // 未开始，50%透明度
            }

            var color = Color.FromArgb((byte)(charOpacity * 255), 255, 255, 255);
            var charStr = text[i].ToString();

            using var charFormat = new CanvasTextFormat
            {
                FontFamily = FontFamilyName,
                FontSize = format.FontSize,
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
            };

            ds.DrawText(
                charStr,
                new Vector2(x + (float)region.LayoutBounds.X, y + (float)region.LayoutBounds.Y),
                color,
                charFormat
            );
        }
    }

    private static void DrawBlurredText(
        CanvasDrawingSession ds,
        string text,
        float x,
        float y,
        float textWidth,
        CanvasTextFormat format,
        Color color,
        int blurAmount
    )
    {
        // 通过多次偏移绘制模拟模糊效果
        var alpha = color.A / 255f;
        var steps = Math.Min(blurAmount, 4);
        var stepAlpha = alpha / (steps * 4 + 1);

        using var layout = new CanvasTextLayout(ds, text, format, textWidth, float.MaxValue);

        for (var dx = -steps; dx <= steps; dx++)
        {
            for (var dy = -steps; dy <= steps; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                var offsetColor = Color.FromArgb(
                    (byte)(stepAlpha * 255),
                    color.R,
                    color.G,
                    color.B
                );
                ds.DrawTextLayout(layout, x + dx * 0.5f, y + dy * 0.5f, offsetColor);
            }
        }

        // 中心绘制主文本
        var centerColor = Color.FromArgb((byte)(alpha * 255 * 0.6f), color.R, color.G, color.B);
        ds.DrawTextLayout(layout, x, y, centerColor);
    }

    private void DrawTranslation(
        CanvasDrawingSession ds,
        string text,
        float y,
        float canvasWidth,
        float fontSize,
        float opacity,
        int blurAmount
    )
    {
        using var format = new CanvasTextFormat
        {
            FontFamily = FontFamilyName,
            FontSize = fontSize,
            HorizontalAlignment =
                canvasWidth > 820
                    ? CanvasHorizontalAlignment.Left
                    : CanvasHorizontalAlignment.Center,
            WordWrapping = CanvasWordWrapping.Wrap,
        };

        var textWidth = canvasWidth - 40f;
        var x = 20f;
        var color = Color.FromArgb((byte)(opacity * 255), 200, 200, 200);

        if (blurAmount > 0)
        {
            DrawBlurredText(ds, text, x, y, textWidth, format, color, blurAmount);
        }
        else
        {
            using var layout = new CanvasTextLayout(ds, text, format, textWidth, float.MaxValue);
            ds.DrawTextLayout(layout, x, y, color);
        }
    }

    private static void DrawHoverBackground(
        CanvasDrawingSession ds,
        float x,
        float y,
        float width,
        float height
    )
    {
        using var geometry = CanvasGeometry.CreateRoundedRectangle(
            ds,
            new Rect(x, y, width, height),
            HoverBorderRadius,
            HoverBorderRadius
        );
        ds.FillGeometry(geometry, Color.FromArgb(40, 255, 255, 255));
        ds.DrawGeometry(geometry, Color.FromArgb(60, 255, 255, 255), 1f);
    }

    private void UpdateLineMeasurements(CanvasDrawingSession ds, float canvasWidth)
    {
        _lineMeasurements.Clear();
        _totalContentHeight = 0;

        using var format = new CanvasTextFormat
        {
            FontFamily = FontFamilyName,
            FontSize = BaseFontSize,
            WordWrapping = CanvasWordWrapping.Wrap,
        };

        var textWidth = canvasWidth - 40f;
        if (textWidth <= 0)
        {
            textWidth = 500f;
        }

        for (var i = 0; i < _renderState.Lines.Count; i++)
        {
            var line = _renderState.Lines[i];
            var distanceFromCenter = Math.Abs(i - _lyricManager.CurrentLyricIndex);
            var fontSize = BlurHelper.CalculateFontSize(
                distanceFromCenter,
                BaseFontSize,
                MinFontSize
            );

            format.FontSize = fontSize;
            using var mainLayout = new CanvasTextLayout(
                ds,
                line.MainText,
                format,
                textWidth,
                float.MaxValue
            );
            var mainHeight = (float)mainLayout.LayoutBounds.Height;

            var translationHeight = 0f;
            if (!string.IsNullOrEmpty(line.TranslationText))
            {
                format.FontSize = fontSize * TranslationFontScale;
                using var transLayout = new CanvasTextLayout(
                    ds,
                    line.TranslationText,
                    format,
                    textWidth,
                    float.MaxValue
                );
                translationHeight = (float)transLayout.LayoutBounds.Height + TranslationSpacing;
            }

            var totalHeight = mainHeight + translationHeight;
            _lineMeasurements[i] = new LineMeasurement(mainHeight, translationHeight, totalHeight);
            _totalContentHeight += totalHeight + LineSpacing;
        }

        _measurementsDirty = false;
    }

    private float CalculateScrollTargetForLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _renderState.Lines.Count)
        {
            return 0;
        }

        // 如果 measurements 还没有初始化，使用估计值
        if (_lineMeasurements.Count == 0)
        {
            // 使用估计的行高：基础字体大小 * 1.5
            var estimatedHeight = BaseFontSize * 1.5f;
            return lineIndex * (estimatedHeight + LineSpacing);
        }

        var targetY = 0f;
        for (var i = 0; i < lineIndex; i++)
        {
            var measurement = _lineMeasurements.GetValueOrDefault(i);
            // 如果某行的 measurement 无效，使用估计值
            var height =
                measurement.TotalHeight > 0 ? measurement.TotalHeight : BaseFontSize * 1.5f;
            targetY += height + LineSpacing;
        }

        return targetY;
    }

    #region 鼠标事件处理

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_renderState.Lines.Count == 0)
        {
            return;
        }

        var point = e.GetCurrentPoint(Canvas);
        var y = (float)point.Position.Y;

        // 计算悬停的行
        var scrollOffset = _renderState.ScrollAnimator.CurrentValue + _renderState.UserScrollOffset;
        var centerY = (float)Canvas!.ActualHeight / 2f;
        var currentY = centerY - scrollOffset;

        _renderState.HoveredLineIndex = -1;
        for (var i = 0; i < _renderState.Lines.Count; i++)
        {
            var measurement = _lineMeasurements.GetValueOrDefault(i);
            var lineHeight = measurement.TotalHeight;

            if (
                y >= currentY - HoverBorderPadding
                && y <= currentY + lineHeight + HoverBorderPadding
            )
            {
                _renderState.HoveredLineIndex = i;
                break;
            }

            currentY += lineHeight + LineSpacing;
        }

        _renderState.RecordInteraction();
    }

    private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _renderState.HoveredLineIndex = -1;
    }

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (
            _renderState.HoveredLineIndex >= 0
            && _renderState.HoveredLineIndex < _renderState.Lines.Count
        )
        {
            var line = _renderState.Lines[_renderState.HoveredLineIndex];
            if (line.SourceSlice is not null)
            {
                LineClicked?.Invoke(this, line.SourceSlice);
            }
        }
    }

    private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);
        var delta = point.Properties.MouseWheelDelta;

        // 用户滚动
        _renderState.UserScrollOffset -= delta * 0.5f;
        _renderState.RecordInteraction();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 加载歌词
    /// </summary>
    public void LoadLyrics()
    {
        _renderState.LoadFromSlices(_lyricManager.CurrentLyricSlices);
        _measurementsDirty = true;
    }

    /// <summary>
    /// 更新设置
    /// </summary>
    public void UpdateSettings()
    {
        BaseFontSize = (float)Settings.LyricPageCurrentFontSize;
        FontFamilyName = Settings.FontFamily.Source;
        _measurementsDirty = true;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void Reset()
    {
        _renderState.Reset();
        _lineMeasurements.Clear();
        _measurementsDirty = true;
    }

    #endregion

    private void AdvancedLyricRenderer_Unloaded(object sender, RoutedEventArgs e)
    {
        Canvas.RemoveFromVisualTree();
        Canvas = null;
        _lineMeasurements.Clear();
    }

    private readonly record struct LineMeasurement(
        float MainTextHeight,
        float TranslationHeight,
        float TotalHeight
    );
}
