namespace UntamedMediaPlayer.Contracts.Activation;

internal interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
