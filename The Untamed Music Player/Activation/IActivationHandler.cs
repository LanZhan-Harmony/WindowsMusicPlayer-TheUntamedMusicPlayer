namespace The_Untamed_Music_Player.Activation;

public interface IActivationHandler
{
    bool CanHandle(object args);
    Task HandleAsync(object args);
}
