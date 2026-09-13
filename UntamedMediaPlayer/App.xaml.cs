using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Activation;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Pages;
using UntamedMediaPlayer.Services;

namespace UntamedMediaPlayer;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;
    public static MainWindow MainWindow { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        _services = ConfigureServices();
        Ioc.Default.ConfigureServices(_services);
        UnhandledException += App_UnhandledException;
    }

    private static ServiceProvider ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection()
            .AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>()
            // Services
            .AddSingleton<IActivationService, ActivationService>()
            .AddSingleton<INavigationService, NavigationService>()
            // Pages
            .AddTransient<NavigationHost>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow = new MainWindow();
        Ioc.Default.GetRequiredService<IActivationService>().ActivateAsync(args);
    }

    /// <summary>
    /// Invoked when the application encounters an unhandled exception.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void App_UnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        // TODO: Log and handle exceptions as appropriate.
    }
}
