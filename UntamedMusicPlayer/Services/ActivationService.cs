using UntamedMusicPlayer.Activation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Core.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using ZLinq;

namespace UntamedMusicPlayer.Services;

public sealed class ActivationService(IEnumerable<IActivationHandler> activationHandlers)
    : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers = activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService =
        App.GetService<IThemeSelectorService>();
    private readonly IMaterialSelectorService _materialSelectorService =
        App.GetService<IMaterialSelectorService>();
    private readonly IDynamicBackgroundService _dynamicBackgroundService =
        App.GetService<IDynamicBackgroundService>();
    private readonly MusicLibrary _musicLibrary = App.GetService<MusicLibrary>();
    private readonly OnlineMusicLibrary _onlineMusicLibrary = App.GetService<OnlineMusicLibrary>();
    private readonly PlaylistLibrary _playlistLibrary = App.GetService<PlaylistLibrary>();
    private readonly MusicPlayer _musicPlayer = App.GetService<MusicPlayer>();

    public async Task ActivateAsync(object activationArgs)
    {
        await Settings.InitializeAsync(); // 初始化设置
        App.MainWindow = new MainWindow();
        await InitializeAsync(); // 在激活之前执行的任务
        await HandleActivationAsync(activationArgs); // 通过 ActivationHandlers 处理激活
        App.MainWindow.Activate(); // 打开 MainWindow
        await StartupAsync(); // 在激活之后执行的任务
    }

    private async Task InitializeAsync()
    {
        _ = _musicLibrary;
        _ = _onlineMusicLibrary;
        _ = _playlistLibrary;
        _ = _musicPlayer;
        _themeSelectorService.Initialize();
        await _materialSelectorService.InitializeAsync();
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
        await _dynamicBackgroundService.InitializeAsync();
    }
}
