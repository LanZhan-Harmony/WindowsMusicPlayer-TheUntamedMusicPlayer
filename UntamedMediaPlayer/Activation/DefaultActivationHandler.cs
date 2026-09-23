using Microsoft.UI.Xaml;
using UntamedMediaPlayer.Contracts.Activation;
using UntamedMediaPlayer.Core.Contracts.Services;

namespace UntamedMediaPlayer.Activation;

internal class DefaultActivationHandler(INavigationService navigationService)
    : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService = navigationService;

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return false;
    }

    protected override async ValueTask HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await ValueTask.CompletedTask;
    }
}
