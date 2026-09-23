using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Contracts.Services;
using UntamedMediaPlayer.Core.Contracts.Services;
using UntamedMediaPlayer.Pages;

namespace UntamedMediaPlayer.Activation;

internal class ActivationService(
    ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
    IEnumerable<IActivationHandler> activationHandlers
) : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler = defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;

    public async ValueTask ActivateAsync(object activationArgs)
    {
        await InitializeAsync();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    /// <summary>
    /// Handles the activation of the application.
    /// </summary>
    /// <param name="activationArgs">The arguments for the activation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async ValueTask HandleActivationAsync(object activationArgs)
    {
        IActivationHandler? activationHandler = _activationHandlers.FirstOrDefault(h =>
            h.CanHandle(activationArgs)
        );

        if (activationHandler is not null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    /// <summary>
    /// Executes tasks before the application is activated
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private ValueTask InitializeAsync()
    {
        App.MainWindow.MainContentView.Content = Ioc.Default.GetRequiredService<NavigationHost>();
        // Load settings while the logging service is already available with defaults.
        // SettingsService then binds DeveloperSettings and enables live log reconfiguration.
        Ioc.Default.GetRequiredService<ISettingsService>();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes tasks after the application has been activated
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private ValueTask StartupAsync()
    {
        return ValueTask.CompletedTask;
    }
}
