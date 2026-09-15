using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace UntamedMediaPlayer;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{
    public ContentPresenter MainContentView => MainContent;

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/Icon.ico"));
        Title = Strings.Resources.AppDisplayName;
    }
}
