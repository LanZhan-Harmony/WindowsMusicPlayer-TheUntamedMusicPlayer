using Windows.UI;

namespace The_Untamed_Music_Player.Contracts.Services;

public interface IMaterialSelectorService
{
    MaterialType Material { get; }
    bool IsFallBack { get; }
    byte LuminosityOpacity { get; }
    Color TintColor { get; }
    Task InitializeAsync();
    Task<(byte, Color)> SetMaterial(MaterialType material);
    void SetIsFallBack(bool isFallBack);
    void SetLuminosityOpacity(byte opacity);
    void SetTintColor(Color color);
}

public enum MaterialType
{
    None = 0,
    Mica = 1,
    MicaAlt = 2,
    DesktopAcrylic = 3,
    AcrylicBase = 4,
    AcrylicThin = 5,
    Blur = 6,
    Transparent = 7,
    Animated = 8,
}
