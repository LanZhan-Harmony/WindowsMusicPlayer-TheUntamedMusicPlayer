using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Activation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.Services;
using The_Untamed_Music_Player.ViewModels;
using WinUIEx;
using ZLogger;

namespace The_Untamed_Music_Player;

public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    // 应用程序级日志记录器
    private static readonly ILogger<App> _logger = LoggingService.CreateLogger<App>();

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException(
                $"{typeof(T)} 需要在 App.xaml.cs 的 ConfigureServices 中注册"
            );
        }

        return service;
    }

    public static WindowEx? MainWindow { get; private set; }

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        // 初始化日志服务（必须在任何日志记录之前）
        LoggingService.Initialize();

        Host = Microsoft
            .Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(
                (context, services) => //注册服务信息
                {
                    // 日志服务注册
                    services.AddSingleton(_ => LoggingService.LoggerFactory);
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

                    // Other Activation Handlers
                    services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();
                    services.AddTransient<IActivationHandler, FileActivationHandler>();

                    // Services
                    services.AddSingleton<IAppNotificationService, AppNotificationService>();
                    services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                    services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                    services.AddSingleton<IMaterialSelectorService, MaterialSelectorService>();
                    services.AddSingleton<IColorExtractionService, ColorExtractionService>();
                    services.AddSingleton<IDynamicBackgroundService, DynamicBackgroundService>();
                    services.AddSingleton<IActivationService, ActivationService>();

                    // Views and ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<RootPlayBarViewModel>();
                    services.AddSingleton<ShellViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<HomeViewModel>();
                    services.AddTransient<MusicLibraryViewModel>();
                    services.AddTransient<PlayQueueViewModel>();
                    services.AddSingleton<PlayListsViewModel>();
                    services.AddTransient<LyricViewModel>();
                    services.AddTransient<LocalSongsViewModel>();
                    services.AddSingleton<LocalAlbumsViewModel>();
                    services.AddSingleton<LocalArtistsViewModel>();
                    services.AddTransient<LocalAlbumDetailViewModel>();
                    services.AddTransient<LocalArtistDetailViewModel>();
                    services.AddTransient<PlayListDetailViewModel>();
                    services.AddTransient<OnlineSongsViewModel>();
                    services.AddTransient<OnlineAlbumsViewModel>();
                    services.AddTransient<OnlineArtistsViewModel>();
                    services.AddTransient<OnlinePlayListsViewModel>();
                    services.AddSingleton<OnlineAlbumDetailViewModel>();
                    services.AddSingleton<OnlineArtistDetailViewModel>();
                    services.AddSingleton<OnlinePlayListDetailViewModel>();
                    services.AddTransient<DesktopLyricViewModel>();

                    // Configuration
                    services
                        .AddOptions<LocalSettingsOptions>()
                        .Configure<IConfiguration>(
                            (settings, configuration) =>
                            {
                                var section = configuration.GetSection(
                                    nameof(LocalSettingsOptions)
                                );
                                // 手动绑定其他属性
                            }
                        );
                }
            )
            .Build(); //生成容器

        GetService<IAppNotificationService>().Initialize();
        UnhandledException += App_UnhandledException;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow = new MainWindow();
        await GetService<IActivationService>().ActivateAsync(args);
    }

    private void App_UnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        var exception = e.Exception;
        var errorMessage = $"未处理的异常: {exception.Message}";

        // 记录详细的异常信息到日志
        _logger.UnexpectedException(errorMessage, exception);

        // 记录堆栈跟踪和内部异常
        _logger.ZLogInformation(
            exception,
            $"异常详细信息: {exception.GetType().Name}, 堆栈跟踪: {exception.StackTrace}"
        );
        if (exception.InnerException is not null)
        {
            _logger.ZLogInformation(
                exception.InnerException,
                $"内部异常: {exception.InnerException.Message}"
            );
        }
        e.Handled = true;
    }
}
