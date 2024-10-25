using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Views;
using WinRT;

namespace The_Untamed_Music_Player.ViewModels;
public class DesktopLyricViewModel
{
    private readonly DesktopLyricWindow? _desktopLyricWindow;
    private readonly ICompositionSupportsSystemBackdrop? _backdropTarget;
    private ISystemBackdropControllerWithTargets? _currentBackdropController;
    private SystemBackdropConfiguration _configurationSource = new()
    {
        IsInputActive = true,
    };

    public DesktopLyricViewModel()
    {
        _desktopLyricWindow = MusicPlayer.DesktopLyricWindow;
        _backdropTarget = _desktopLyricWindow.As<ICompositionSupportsSystemBackdrop>();

    }

    public void ChangeMaterial()
    {
        _currentBackdropController?.RemoveAllSystemBackdropTargets();
        _currentBackdropController?.Dispose();
        _currentBackdropController = new MicaController { Kind = MicaKind.BaseAlt };
    }
}
