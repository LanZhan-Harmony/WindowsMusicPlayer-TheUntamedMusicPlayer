namespace The_Untamed_Music_Player.Contracts.Services;
public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
