namespace UntamedMediaPlayer.Contracts.Services;

internal interface IActivationService
{
    ValueTask ActivateAsync(object activationArgs);
}
