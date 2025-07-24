using The_Untamed_Music_Player.Activation;
using The_Untamed_Music_Player.Contracts.Services;

namespace The_Untamed_Music_Player.Services;

public class ActivationService(
    IEnumerable<IActivationHandler> activationHandlers,
    IThemeSelectorService themeSelectorService
) : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow?.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h =>
            h.CanHandle(activationArgs)
        );

        if (activationHandler is not null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }
}
