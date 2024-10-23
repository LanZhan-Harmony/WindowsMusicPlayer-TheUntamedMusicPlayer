using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI.ViewManagement;
using WinRT;
namespace The_Untamed_Music_Player;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
    private readonly UISettings settings;
    private readonly ILocalSettingsService _localSettingsService = App.GetService<ILocalSettingsService>();
    public MainViewModel ViewModel
    {
        get;
    }
    private WindowsSystemDispatcherQueueHelper? m_wsdqHelper;
    public DesktopAcrylicController? m_acrylicController;
    private SystemBackdropConfiguration? m_configurationSource;

    private byte _selectedMaterial;
    /// <summary>
    /// 选定的窗口材质
    /// </summary>
    public byte SelectedMaterial
    {
        get => _selectedMaterial;
        set => _selectedMaterial = value;
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializeAsync();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Title = "AppDisplayName".GetLocalized();
        ExtendsContentIntoTitleBar = true;

        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;

        Data.MainWindow = this;

        ShellFrame.Navigate(typeof(ShellPage));
        RootPlayBarFrame.Navigate(typeof(RootPlayBarView));

        //ViewModel = App.GetService<MainViewModel>();
        Activated += Window_Activated;
        Closed += Window_Closed;
    }

    /// <summary>
    /// 初始化窗口材质
    /// </summary>
    public async void InitializeAsync()
    {
        await LoadSelectedMaterialAsync();
        ChangeMaterial(SelectedMaterial);
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
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }

    /// <summary>
    /// 尝试设置系统背景为无
    /// </summary>
    /// <returns></returns>
    public bool TrySetNoneBackdrop()
    {
        try
        {
            SystemBackdrop = null;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置Mica背景
    /// </summary>
    /// <param name="useMicaAlt"></param>
    /// <returns></returns>
    public bool TrySetMicaBackdrop(bool useMicaAlt = false)
    {
        try
        {
            if (MicaController.IsSupported())
            {
                var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop
                {
                    Kind = useMicaAlt ? MicaKind.BaseAlt : MicaKind.Base
                };
                SystemBackdrop = micaBackdrop;

                return true; // Succeeded.
            }
            return false; // Mica is not supported on this system.
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置桌面亚克力背景
    /// </summary>
    /// <returns></returns>
    public bool TrySetDesktopAcrylicBackdrop()
    {
        try
        {
            if (DesktopAcrylicController.IsSupported())
            {
                var DesktopAcrylicBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                SystemBackdrop = DesktopAcrylicBackdrop;

                return true; // 成功
            }
            return false; // 桌面亚克力不受此系统支持
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置亚克力背景
    /// </summary>
    /// <param name="useAcrylicThin"></param>
    /// <returns></returns>
    public bool TrySetAcrylicBackdrop(bool useAcrylicThin = false)
    {
        try
        {
            if (DesktopAcrylicController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // 挂钩策略对象到窗口
                m_configurationSource = new SystemBackdropConfiguration();
                ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

                // 初始化配置状态
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_acrylicController = new DesktopAcrylicController
                {
                    Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
                };

                // 启用系统背景
                // 注意：确保有“using WinRT;”以支持Window.As<...>()调用。
                m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // 成功
            }

            return false; // 亚克力不受此系统支持
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置模糊背景
    /// </summary>
    /// <returns></returns>
    public bool TrySetBlurBackdrop()
    {
        try
        {
            m_acrylicController?.Dispose();
            m_acrylicController = null;
            var blurBackdrop = new BlurredBackdrop();
            SystemBackdrop = blurBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    /// 尝试设置透明背景
    /// </summary>
    /// <param name="tintColor"></param>
    /// <returns></returns>
    public bool TrySetTransparentBackdrop(Windows.UI.Color? tintColor = null)
    {
        try
        {
            m_acrylicController?.Dispose();
            m_acrylicController = null;
            var color = tintColor ?? Colors.Transparent;
            var transparentBackdrop = new TransparentTintBackdrop(color);
            SystemBackdrop = transparentBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试设置动画背景
    /// </summary>
    /// <returns></returns>
    public bool TrySetAnimatedBackdrop()
    {
        try
        {
            m_acrylicController?.Dispose();
            m_acrylicController = null;
            var animatedBackdrop = new ColorAnimatedBackdrop();
            SystemBackdrop = animatedBackdrop;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (m_configurationSource != null)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // 确保任何Mica/Acrylic控制器都被释放，以便它不会尝试使用此关闭的窗口。
        m_acrylicController?.Dispose();
        m_acrylicController = null;

        Activated -= Window_Activated;
        m_configurationSource = null;

        Data.MusicPlayer.Player.Dispose();
        Data.MusicPlayer.SaveCurrentStateAsync();
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (m_configurationSource != null)
        {
            SetConfigurationSourceTheme();
        }
    }

    private void SetConfigurationSourceTheme()
    {
        if (m_configurationSource != null)
        {
            switch (((FrameworkElement)Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
    }

    /// <summary>
    /// 启动应用时更改选定的材质
    /// </summary>
    /// <param name="material"></param>
    public void ChangeMaterial(byte material)
    {
        try
        {
            switch (material)
            {
                case 0:
                    TrySetNoneBackdrop();
                    break;
                case 1:
                    TrySetMicaBackdrop(false);
                    break;
                case 2:
                    TrySetMicaBackdrop(true);
                    break;
                case 3:
                    TrySetDesktopAcrylicBackdrop();
                    break;
                case 4:
                    TrySetAcrylicBackdrop(false);
                    break;
                case 5:
                    TrySetAcrylicBackdrop(true);
                    break;
                case 6:
                    TrySetBlurBackdrop();
                    break;
                case 7:
                    TrySetTransparentBackdrop();
                    break;
                case 8:
                    TrySetAnimatedBackdrop();
                    break;
            }
        }
        catch { }
    }

    /// <summary>
    /// 从设置存储读取选定的材质
    /// </summary>
    /// <returns></returns>
    public async Task LoadSelectedMaterialAsync()
    {
        SelectedMaterial = await _localSettingsService.ReadSettingAsync<bool>("NotFirstUsed")
            ? await _localSettingsService.ReadSettingAsync<byte>("SelectedMaterial")
            : (byte)3;
    }
}
