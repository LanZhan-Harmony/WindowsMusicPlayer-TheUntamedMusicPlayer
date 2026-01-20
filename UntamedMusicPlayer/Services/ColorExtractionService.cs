using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using UntamedMusicPlayer.Contracts.Services;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Streams;
using Windows.UI;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Services;

/// <summary>
/// 颜色提取服务，使用八叉树算法从图片中提取主色调
/// </summary>
public sealed class ColorExtractionService : IColorExtractionService
{
    private readonly ILogger _logger = LoggingService.CreateLogger<ColorExtractionService>();

    /// <summary>
    /// 从字节数组中提取主色调
    /// </summary>
    /// <param name="imageBytes">图像字节数组</param>
    /// <param name="maxColors">最大颜色数量</param>
    /// <returns>主色调列表</returns>
    public async Task<List<Color>> ExtractColorsAsync(byte[] imageBytes, int maxColors = 8)
    {
        try
        {
            var device = CanvasDevice.GetSharedDevice();
            using var stream = new InMemoryRandomAccessStream();

            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            using var bitmap = await CanvasBitmap.LoadAsync(device, stream);

            // 缩放图像以提高性能
            var scaledSize = GetScaledSize(
                new SizeInt32
                {
                    Width = (int)bitmap.SizeInPixels.Width,
                    Height = (int)bitmap.SizeInPixels.Height,
                },
                100
            );
            using var scaledBitmap = ResizeBitmap(device, bitmap, scaledSize);

            var pixels = scaledBitmap.GetPixelColors();

            return ExtractColorsUsingOctree(pixels, maxColors);
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

    private static SizeInt32 GetScaledSize(SizeInt32 originalSize, int maxDimension)
    {
        var scale = Math.Min(
            (float)maxDimension / originalSize.Width,
            (float)maxDimension / originalSize.Height
        );
        return scale >= 1
            ? originalSize
            : new SizeInt32
            {
                Width = (int)(originalSize.Width * scale),
                Height = (int)(originalSize.Height * scale),
            };
    }

    private static CanvasBitmap ResizeBitmap(
        CanvasDevice device,
        CanvasBitmap original,
        SizeInt32 newSize
    )
    {
        using var renderTarget = new CanvasRenderTarget(device, newSize.Width, newSize.Height, 96);
        using var session = renderTarget.CreateDrawingSession();
        session.DrawImage(original, new Rect(0, 0, newSize.Width, newSize.Height));
        return CanvasBitmap.CreateFromDirect3D11Surface(device, renderTarget);
    }

    private static List<Color> ExtractColorsUsingOctree(Color[] pixels, int maxColors)
    {
        var root = new OctreeNode();
        OctreeNode.LeafCount = 0;
        OctreeNode.ReducibleNodes = new List<OctreeNode>[8];
        for (var i = 0; i < 8; i++)
        {
            OctreeNode.ReducibleNodes[i] = [];
        }

        // 添加像素到八叉树
        foreach (var pixel in pixels)
        {
            // 跳过透明像素
            if (pixel.A < 128)
            {
                continue;
            }

            root.AddColor(pixel, 0);

            while (OctreeNode.LeafCount > maxColors)
            {
                ReduceTree();
            }
        }

        // 收集颜色
        var colorStats = new Dictionary<Color, int>();
        CollectColors(root, colorStats);

        return
        [
            .. colorStats
                .AsValueEnumerable()
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxColors)
                .Select(kvp => kvp.Key),
        ];
    }

    private static void ReduceTree()
    {
        // 找到最深层的可归约节点
        var level = 6;
        while (level >= 0 && OctreeNode.ReducibleNodes[level].Count == 0)
        {
            level--;
        }

        if (level < 0)
        {
            return;
        }

        var node = OctreeNode.ReducibleNodes[level][^1];
        OctreeNode.ReducibleNodes[level].RemoveAt(OctreeNode.ReducibleNodes[level].Count - 1);

        // 合并子节点
        node.IsLeaf = true;
        node.Red = 0;
        node.Green = 0;
        node.Blue = 0;
        node.PixelCount = 0;

        foreach (var child in node.Children)
        {
            if (child is not null)
            {
                node.Red += child.Red;
                node.Green += child.Green;
                node.Blue += child.Blue;
                node.PixelCount += child.PixelCount;
                OctreeNode.LeafCount--;
            }
        }

        OctreeNode.LeafCount++;
        Array.Clear(node.Children);
    }

    private static void CollectColors(OctreeNode node, Dictionary<Color, int> colorStats)
    {
        if (node.IsLeaf)
        {
            if (node.PixelCount > 0)
            {
                var r = (byte)(node.Red / node.PixelCount);
                var g = (byte)(node.Green / node.PixelCount);
                var b = (byte)(node.Blue / node.PixelCount);
                var color = Color.FromArgb(255, r, g, b);
                colorStats[color] = node.PixelCount;
            }
            return;
        }

        foreach (var child in node.Children)
        {
            if (child is not null)
            {
                CollectColors(child, colorStats);
            }
        }
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
/// 八叉树节点
/// </summary>
internal class OctreeNode
{
    public static int LeafCount = 0;
    public static List<OctreeNode>[] ReducibleNodes = null!;

    public OctreeNode?[] Children { get; } = new OctreeNode?[8];
    public bool IsLeaf { get; set; } = false;
    public int Red { get; set; } = 0;
    public int Green { get; set; } = 0;
    public int Blue { get; set; } = 0;
    public int PixelCount { get; set; } = 0;

    public OctreeNode(int level = -1)
    {
        if (level == 7)
        {
            IsLeaf = true;
            LeafCount++;
        }
        else if (level >= 0)
        {
            ReducibleNodes[level].Add(this);
        }
    }

    public void AddColor(Color color, int level)
    {
        if (IsLeaf)
        {
            PixelCount++;
            Red += color.R;
            Green += color.G;
            Blue += color.B;
        }
        else
        {
            var index = GetColorIndex(color, level);

            Children[index] ??= new OctreeNode(level + 1);
            Children[index]!.AddColor(color, level + 1);
        }
    }

    private static int GetColorIndex(Color color, int level)
    {
        var index = 0;
        var mask = 0x80 >> level;

        if ((color.R & mask) != 0)
        {
            index |= 4;
        }

        if ((color.G & mask) != 0)
        {
            index |= 2;
        }

        if ((color.B & mask) != 0)
        {
            index |= 1;
        }

        return index;
    }
}

/// <summary>
/// 渐变配置
/// </summary>
public sealed record GradientConfig(List<Color> Colors, double Angle);
