using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Contracts.Services;
using UntamedMediaPlayer.Pages;

namespace UntamedMediaPlayer.Activation;

internal class ActivationService(
    ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
    IEnumerable<IActivationHandler> activationHandlers
) : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler = defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;

    public async Task ActivateAsync(object activationArgs)
    {
        await InitializeAsync();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
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

    private Task InitializeAsync()
    {
        App.MainWindow.MainContentView.Content = Ioc.Default.GetRequiredService<NavigationHost>();
        return Task.CompletedTask;
    }

    private Task StartupAsync() => Task.CompletedTask;
}
