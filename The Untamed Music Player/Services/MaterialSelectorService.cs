using The_Untamed_Music_Player.Contracts.Services;
using Windows.UI;

namespace The_Untamed_Music_Player.Services;

public class MaterialSelectorService : IMaterialSelectorService
{
    public MaterialType Material { get; set; }

    public bool IsFallBack { get; set; }

    public byte LuminosityOpacity { get; set; }

    public Color BackgroundColor { get; set; }

    public Task SetBackgroundColor(Color color) => throw new NotImplementedException();

    public Task SetIsFallBack(bool isFallBack) => throw new NotImplementedException();

    public Task SetLuminosityOpacity(byte opacity) => throw new NotImplementedException();

    public Task SetMaterial(MaterialType material) => throw new NotImplementedException();
}
