using Microsoft.UI.Xaml;
using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.ViewModels;

namespace The_Untamed_Music_Player.Activation;
public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content is null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(HomeViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
