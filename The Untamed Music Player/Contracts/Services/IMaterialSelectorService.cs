using Windows.UI;

namespace The_Untamed_Music_Player.Contracts.Services;

public interface IMaterialSelectorService : IDisposable
{
    MaterialType Material { get; set; }
    bool IsFallBack { get; set; }
    byte LuminosityOpacity { get; set; }
    Color TintColor { get; set; }
    Task InitializeAsync();
    Task<(byte, Color)> SetMaterial(
        MaterialType material,
        bool firstStart = false,
        bool forced = false
    );
    void SetLuminosityOpacity(byte opacity, bool firstStart = false);
    void SetTintColor(Color color, bool firstStart = false);
}

public enum MaterialType
{
    /// <summary>
    /// 无背景
    /// </summary>
    None = 0,

    /// <summary>
    /// 云母
    /// </summary>
    Mica = 1,

    /// <summary>
    /// 云母Alt
    /// </summary>
    MicaAlt = 2,

    /// <summary>
    /// 桌面亚克力
    /// </summary>
    DesktopAcrylic = 3,

    /// <summary>
    /// 基础亚克力
    /// </summary>
    AcrylicBase = 4,

    /// <summary>
    /// 薄亚克力
    /// </summary>
    AcrylicThin = 5,

    /// <summary>
    /// 模糊
    /// </summary>
    Blur = 6,

    /// <summary>
    /// 透明
    /// </summary>
    Transparent = 7,

    /// <summary>
    /// 变色
    /// </summary>
    Animated = 8,
}
