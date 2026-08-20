using UntamedMusicPlayer.Services;
using Windows.UI;

namespace UntamedMusicPlayer.Contracts.Services;

/// <summary>
/// 颜色提取服务接口
/// </summary>
public interface IColorExtractionService
{
    Task<List<Color>> ExtractColorsAsync(byte[] imageBytes, int maxColors = 8);

    Task<List<Color>> ExtractColorsAsync(string imageUrl, int maxColors = 8);

    GradientConfig GenerateGradient(List<Color> colors);

    Color CalculateAccentColor(List<Color> colors);
}
