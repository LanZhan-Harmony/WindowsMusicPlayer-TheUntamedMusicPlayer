using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Controls;

namespace UntamedMusicPlayer.Contracts.Services;

public interface IWindowService
{
    MainWindow? MainWindow { get; }
    bool IsFullScreen { get; }

    void Initialize(MainWindow mainWindow);

    void SetTitleBar(UIElement titleBar);

    Grid? GetBackgroundGrid();

    void AddImageViewerWindow(Guid windowId, ImageViewerWindow window);

    void RemoveImageViewerWindow(Guid windowId);

    void CloseImageViewerWindows();

    void ShowDesktopLyricWindow(Action closedCallback);

    void CloseDesktopLyricWindow();

    void ToggleFullScreen();
}
