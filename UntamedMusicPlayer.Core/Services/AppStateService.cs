using UntamedMusicPlayer.Core.Contracts.Services;

namespace UntamedMusicPlayer.Core.Services;

public sealed class AppStateService : IAppStateService
{
    public bool IsMusicProcessing { get; set; }

    public bool IsFileActivationLaunch { get; set; }
}