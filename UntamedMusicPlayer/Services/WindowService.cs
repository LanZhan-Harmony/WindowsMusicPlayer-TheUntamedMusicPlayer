using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Services;

public sealed class WindowService : IWindowService
{
    private MainWindow? _mainWindow;
    private readonly Dictionary<Guid, ImageViewerWindow> _imageViewerWindows = [];
    private DesktopLyricWindow? _desktopLyricWindow;

    public MainWindow? MainWindow => _mainWindow;

    public bool IsFullScreen =>
        _mainWindow?.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;

    public void Initialize(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void SetTitleBar(UIElement titleBar)
    {
        _mainWindow?.SetTitleBar(titleBar);
    }

    public Grid? GetBackgroundGrid() => _mainWindow?.GetBackgroundGrid();

    public void AddImageViewerWindow(Guid windowId, ImageViewerWindow window)
    {
        _imageViewerWindows[windowId] = window;
    }

    public void RemoveImageViewerWindow(Guid windowId)
    {
        _imageViewerWindows.Remove(windowId);
    }

    public void CloseImageViewerWindows()
    {
        foreach (var window in _imageViewerWindows.Values.ToArray())
        {
            window.Dispose();
        }
        _imageViewerWindows.Clear();
    }

    public void ShowDesktopLyricWindow(Action closedCallback)
    {
        CloseDesktopLyricWindow();
        _desktopLyricWindow = new DesktopLyricWindow(() =>
        {
            _desktopLyricWindow = null;
            closedCallback();
        });
    }

    public void CloseDesktopLyricWindow()
    {
        var window = _desktopLyricWindow;
        _desktopLyricWindow = null;
        window?.Dispose();
    }

    public void ToggleFullScreen()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.AppWindow.SetPresenter(
            IsFullScreen ? AppWindowPresenterKind.Default : AppWindowPresenterKind.FullScreen
        );
    }
}
