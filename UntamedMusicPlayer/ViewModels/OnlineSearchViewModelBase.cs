using CommunityToolkit.Mvvm.ComponentModel;
using UntamedMusicPlayer.Core.Services;

namespace UntamedMusicPlayer.ViewModels;

/// <summary>
/// Common application-facing surface for online result pages.
/// </summary>
/// <remarks>
/// The coordinator remains responsible for request and provider selection. Pages only receive
/// their ViewModel and no longer resolve the coordinator from the global service locator.
/// </remarks>
public abstract class OnlineSearchViewModelBase : ObservableObject
{
    protected OnlineSearchViewModelBase(OnlineMusicLibrary onlineLibrary)
    {
        OnlineLibrary = onlineLibrary ?? throw new ArgumentNullException(nameof(onlineLibrary));
    }

    public OnlineMusicLibrary OnlineLibrary { get; }

    public Task SearchMoreAsync() => OnlineLibrary.SearchMore();
}
