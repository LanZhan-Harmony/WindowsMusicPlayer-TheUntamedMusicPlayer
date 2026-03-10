namespace UntamedMusicPlayer.LyricRenderer;

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
