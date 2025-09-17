using The_Untamed_Music_Player.Activation;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.Models;
using ZLinq;

namespace The_Untamed_Music_Player.Services;

public class ActivationService(IEnumerable<IActivationHandler> activationHandlers)
    : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService =
        App.GetService<IThemeSelectorService>();
    private readonly IMaterialSelectorService _materialSelectorService =
        App.GetService<IMaterialSelectorService>();
    private readonly IDynamicBackgroundService _dynamicBackgroundService =
        App.GetService<IDynamicBackgroundService>();

    public async Task ActivateAsync(object activationArgs)
    {
        await InitializeAsync(); // 在激活之前执行的任务
        await HandleActivationAsync(activationArgs); // 通过 ActivationHandlers 处理激活
        App.MainWindow?.Activate(); // 打开 MainWindow
        await StartupAsync(); // 在激活之后执行的任务
    }

    private async Task InitializeAsync()
    {
        await Settings.InitializeAsync().ConfigureAwait(false);
        _themeSelectorService.Initialize();
        _materialSelectorService.InitializeSettings();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers
            .AsValueEnumerable()
            .FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler is not null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }
    }

    private async Task StartupAsync()
    {
        _themeSelectorService.SetRequestedThemeAsync();
        await _materialSelectorService.InitializeMaterialAsync();
        await _dynamicBackgroundService.InitializeAsync();
    }
}
