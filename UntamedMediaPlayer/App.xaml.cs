using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Activation;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Contracts.Services;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Core.Helpers;
using UntamedMediaPlayer.Pages;
using UntamedMediaPlayer.Services;
using ZLogger;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace UntamedMediaPlayer;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        ServiceProvider services = ConfigureServices();
        Ioc.Default.ConfigureServices(services);
        UnhandledException += App_UnhandledException;
    }

    /// <summary>
    /// Configures the services required by the application.
    /// </summary>
    /// <returns>The configured service provider.</returns>
    private static ServiceProvider ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection()
            .AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>()
            // Services
            .AddSingleton<IActivationService, ActivationService>()
            .AddSingleton<INavigationService, NavigationService>()
            .AddSingleton<ILocalizationService, LocalizationService>()
            // Pages
            .AddTransient<NavigationHost>();
        ServicesHelper.ConfigureCoreServices(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow = new MainWindow();
        await Ioc.Default.GetRequiredService<IActivationService>().ActivateAsync(args);
    }

    /// <summary>
    /// Invoked when the application encounters an unhandled exception.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ILogger<App> logger = Ioc.Default.GetRequiredService<ILogger<App>>();
        logger.ZLogError(e.Exception, $"[App] An unhandled exception occurred");
        // e.Handled = true; // Uncomment this line to prevent the application from crashing on unhandled exceptions
    }
}
