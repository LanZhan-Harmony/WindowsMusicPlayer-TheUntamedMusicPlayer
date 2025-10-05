using Microsoft.UI.Xaml;
using UntamedMusicPlayer.Services;
using Windows.UI;

namespace UntamedMusicPlayer.Contracts.Services;

/// <summary>
/// 颜色提取服务接口
/// </summary>
public interface IColorExtractionService
{
    /// <summary>
    /// 从字节数组中提取主色调
    /// </summary>
    /// <param name="imageBytes">图像字节数组</param>
    /// <param name="maxColors">最大颜色数量</param>
    /// <returns>主色调列表</returns>
    Task<List<Color>> ExtractColorsAsync(byte[] imageBytes, int maxColors = 8);

    /// <summary>
    /// 从URL中提取主色调
    /// </summary>
    /// <param name="imageUrl">图像URL</param>
    /// <param name="maxColors">最大颜色数量</param>
    /// <returns>主色调列表</returns>
    Task<List<Color>> ExtractColorsAsync(string imageUrl, int maxColors = 8);

    /// <summary>
    /// 生成渐变色配置
    /// </summary>
    /// <param name="colors">颜色列表</param>
    /// <returns>渐变色配置</returns>
    GradientConfig GenerateGradient(List<Color> colors);

    /// <summary>
    /// 计算强调色（最突出的颜色）
    /// </summary>
    /// <param name="colors">颜色列表</param>
    /// <returns>强调色</returns>
    Color CalculateAccentColor(List<Color> colors);
}

/// <summary>
/// 动态背景服务接口
/// </summary>
public interface IDynamicBackgroundService : IDisposable
{
    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 背景更新事件
    /// </summary>
    event Action<List<Color>>? BackgroundColorsChanged;

    /// <summary>
    /// 初始化动态背景服务
    /// </summary>
    /// <param name="targetElement">目标元素</param>
    Task InitializeAsync(FrameworkElement? targetElement = null);

    /// <summary>
    /// 手动更新背景
    /// </summary>
    /// <returns></returns>
    Task UpdateBackgroundAsync();
}
