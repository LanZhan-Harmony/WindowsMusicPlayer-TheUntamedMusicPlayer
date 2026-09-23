namespace UntamedMediaPlayer.Contracts.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);
    ValueTask HandleAsync(object args);
}
