namespace UntamedMediaPlayer.Contracts.Services;

internal interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
