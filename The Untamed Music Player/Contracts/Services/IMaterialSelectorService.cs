using Windows.UI;

namespace The_Untamed_Music_Player.Contracts.Services;

public interface IMaterialSelectorService
{
    MaterialType Material { get; }
    bool IsFallBack { get; }
    byte LuminosityOpacity { get; }
    Color BackgroundColor { get; }
    Task SetMaterial(MaterialType material);
    Task SetIsFallBack(bool isFallBack);
    Task SetLuminosityOpacity(byte opacity);
    Task SetBackgroundColor(Color color);
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
