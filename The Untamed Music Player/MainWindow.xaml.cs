using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI.ViewManagement;

namespace The_Untamed_Music_Player;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon/WindowIcon.ico"));
        Title = "AppDisplayName".GetLocalized();
        ExtendsContentIntoTitleBar = true;

        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;

        Data.MainWindow = this;

        ShellFrame.Navigate(typeof(ShellPage));
        ViewModel = App.GetService<MainViewModel>();
    }

    /// <summary>
    /// 获取导航页(ShellFrame)
    /// </summary>
    /// <returns></returns>
    public Frame GetShellFrame()
    {
        return ShellFrame;
    }

    /// <summary>
    /// 处理在应用程序打开时主题改变时正确更新标题按钮颜色
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // 这个调用来自线程外，因此我们需要将其调度到当前应用程序的线程
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }
}
