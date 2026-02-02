using ComputeSharp;

namespace UntamedMusicPlayer.LyricRenderer;

/// <summary>
/// 高斯模糊着色器 - 水平方向
/// </summary>
[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct HorizontalBlurShader(
    ReadWriteTexture2D<Rgba32, float4> texture,
    int radius
) : IComputeShader
{
    public void Execute()
    {
        var coords = ThreadIds.XY;
        var width = texture.Width;
        var height = texture.Height;

        if (coords.X >= width || coords.Y >= height)
        {
            return;
        }

        var sum = float4.Zero;
        var count = 0;

        for (var i = -radius; i <= radius; i++)
        {
            var x = coords.X + i;
            if (x >= 0 && x < width)
            {
                sum += texture[x, coords.Y];
                count++;
            }
        }

        texture[coords.XY] = sum / count;
    }
}

/// <summary>
/// 高斯模糊着色器 - 垂直方向
/// </summary>
[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
public readonly partial struct VerticalBlurShader(
    ReadWriteTexture2D<Rgba32, float4> texture,
    int radius
) : IComputeShader
{
    public void Execute()
    {
        var coords = ThreadIds.XY;
        var width = texture.Width;
        var height = texture.Height;

        if (coords.X >= width || coords.Y >= height)
        {
            return;
        }

        var sum = float4.Zero;
        var count = 0;

        for (var i = -radius; i <= radius; i++)
        {
            var y = coords.Y + i;
            if (y >= 0 && y < height)
            {
                sum += texture[(int)coords.X, (int)y];
                count++;
            }
        }

        texture[coords.XY] = sum / count;
    }
}

/// <summary>
/// 模糊效果辅助类
/// </summary>
public static class BlurHelper
{
    /// <summary>
    /// 根据距离中心的偏移计算模糊半径
    /// </summary>
    /// <param name="distanceFromCenter">距离中心的行数</param>
    /// <param name="maxBlurRadius">最大模糊半径</param>
    /// <returns>模糊半径</returns>
    public static int CalculateBlurRadius(int distanceFromCenter, int maxBlurRadius = 8)
    {
        if (distanceFromCenter <= 0)
        {
            return 0;
        }
        // 距离越远，模糊越大，但有上限
        return Math.Min(distanceFromCenter * 2, maxBlurRadius);
    }

    /// <summary>
    /// 根据距离中心的偏移计算字体大小缩放
    /// </summary>
    /// <param name="distanceFromCenter">距离中心的行数</param>
    /// <param name="baseFontSize">基础字体大小</param>
    /// <param name="minFontSize">最小字体大小</param>
    /// <returns>缩放后的字体大小</returns>
    public static float CalculateFontSize(
        int distanceFromCenter,
        float baseFontSize,
        float minFontSize = 16f
    )
    {
        if (distanceFromCenter <= 0)
        {
            return baseFontSize;
        }
        // 每远离一行，字体减小10%，但不低于最小值
        var scale = Math.Max(0.4f, 1f - distanceFromCenter * 0.1f);
        return Math.Max(minFontSize, baseFontSize * scale);
    }

    /// <summary>
    /// 根据距离中心的偏移计算透明度
    /// </summary>
    /// <param name="distanceFromCenter">距离中心的行数</param>
    /// <param name="isCurrent">是否为当前行</param>
    /// <returns>透明度值</returns>
    public static float CalculateOpacity(int distanceFromCenter, bool isCurrent)
    {
        if (isCurrent)
        {
            return 1f;
        }
        // 非当前行基础透明度0.5，距离越远越淡
        return Math.Max(0.2f, 0.5f - distanceFromCenter * 0.05f);
    }
}
