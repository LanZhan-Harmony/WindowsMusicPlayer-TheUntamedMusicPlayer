namespace UntamedMusicPlayer.Core.Contracts.Services;

public interface IAppStateService
{
    bool IsMusicProcessing { get; set; }

    bool IsFileActivationLaunch { get; set; }
}