namespace UntamedMediaPlayer.Contracts.Activation;

internal interface IActivationHandler
{
    bool CanHandle(object args);
    ValueTask HandleAsync(object args);
}
