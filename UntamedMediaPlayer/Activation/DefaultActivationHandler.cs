using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Activation;

internal class DefaultActivationHandler(INavigationService navigationService)
    : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService = navigationService;

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args) =>
        false;

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await Task.CompletedTask;
    }
}
