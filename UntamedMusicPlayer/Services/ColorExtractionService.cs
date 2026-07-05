using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using UntamedMusicPlayer.Contracts.Services;
using Windows.UI;
using ZLinq;
using ZLogger;
using ColorThiefImageSharp = ColorThief.ImageSharp.ColorThief;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace UntamedMusicPlayer.Services;

/// <summary>
/// 颜色提取服务，使用 ColorThief.ImageSharp 从图片中提取主色调
/// </summary>
public sealed class ColorExtractionService : IColorExtractionService
{
    private const int ColorThiefQuality = 10;
    private const bool IgnoreWhite = true;
    private readonly ILogger _logger = LoggingService.CreateLogger<ColorExtractionService>();

    /// <summary>
    /// 从字节数组中提取主色调
    /// </summary>
    /// <param name="imageBytes">图像字节数组</param>
    /// <param name="maxColors">最大颜色数量</param>
    /// <returns>主色调列表</returns>
    public async Task<List<Color>> ExtractColorsAsync(byte[] imageBytes, int maxColors = 8)
    {
        if (imageBytes.Length == 0 || maxColors <= 0)
        {
            return [];
        }

        try
        {
            return await Task.Run(() => ExtractColorsWithColorThief(imageBytes, maxColors));
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"从字节数组中提取颜色失败");
            return []; // 返回空列表而不是抛出异常
        }
    }

    /// <summary>
    /// 从URL中提取主色调
    /// </summary>
    /// <param name="imageUrl">图像URL</param>
    /// <param name="maxColors">最大颜色数量</param>
    /// <returns>主色调列表</returns>
    public async Task<List<Color>> ExtractColorsAsync(string imageUrl, int maxColors = 8)
    {
        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            return await ExtractColorsAsync(imageBytes, maxColors);
        }
        catch (Exception ex)
        {
            _logger.ZLogInformation(ex, $"从URL{imageUrl}提取颜色失败");
            return [];
        }
    }

    /// <summary>
    /// 生成渐变色配置
    /// </summary>
    /// <param name="colors">颜色列表</param>
    /// <returns>渐变色配置</returns>
    public GradientConfig GenerateGradient(List<Color> colors)
    {
        if (colors.Count == 0)
        {
            return new GradientConfig([Color.FromArgb(255, 44, 44, 44)], -45);
        }

        // 按亮度排序
        var sortedColors = colors.AsValueEnumerable().OrderBy(CalculateLuminance).ToArray();

        // 选择中间的颜色用于渐变
        var count = Math.Min(sortedColors.Length, 4);
        var startIndex = Math.Max(0, (sortedColors.Length - count) / 2);
        var gradientColors = sortedColors.AsValueEnumerable().Skip(startIndex).Take(count).ToList();

        return new GradientConfig(gradientColors, -45);
    }

    /// <summary>
    /// 计算强调色（最突出的颜色）
    /// </summary>
    /// <param name="colors">颜色列表</param>
    /// <returns>强调色</returns>
    public Color CalculateAccentColor(List<Color> colors)
    {
        if (colors.Count == 0)
        {
            return Color.FromArgb(255, 0, 120, 215); // 默认蓝色
        }

        // 选择饱和度和亮度适中的颜色作为强调色
        return colors
            .AsValueEnumerable()
            .OrderByDescending(color =>
                CalculateSaturation(color) * (1 - Math.Abs(CalculateLuminance(color) - 0.5))
            )
            .First();
    }

    private static List<Color> ExtractColorsWithColorThief(byte[] imageBytes, int maxColors)
    {
        using var image = ImageSharpImage.Load<Rgba32>(imageBytes);
        var palette = new ColorThiefImageSharp().GetPalette(
            image,
            maxColors,
            ColorThiefQuality,
            IgnoreWhite
        );

        return
        [
            .. palette
                .AsValueEnumerable()
                .OrderByDescending(color => color.Population)
                .Take(maxColors)
                .Select(color => Color.FromArgb(255, color.Color.R, color.Color.G, color.Color.B)),
        ];
    }

    private static double CalculateLuminance(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        return 0.299 * r + 0.587 * g + 0.114 * b;
    }

    private static double CalculateSaturation(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        var max = Math.Max(Math.Max(r, g), b);
        var min = Math.Min(Math.Min(r, g), b);

        return max == 0 ? 0 : (max - min) / max;
    }
}

/// <summary>
/// 渐变配置
/// </summary>
public sealed record GradientConfig(List<Color> Colors, double Angle);
