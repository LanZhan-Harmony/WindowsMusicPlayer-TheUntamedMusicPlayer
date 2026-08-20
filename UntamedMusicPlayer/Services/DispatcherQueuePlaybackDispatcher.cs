using Microsoft.UI.Dispatching;
using UntamedMusicPlayer.Core.Contracts.Services;

namespace UntamedMusicPlayer.Services;

public sealed class DispatcherQueuePlaybackDispatcher : IPlaybackDispatcher
{
    private readonly DispatcherQueue _dispatcher;

    public DispatcherQueuePlaybackDispatcher(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public bool TryEnqueue(Action action, bool highPriority = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _dispatcher.TryEnqueue(
            highPriority ? DispatcherQueuePriority.High : DispatcherQueuePriority.Normal,
            () => action()
        );
    }
}
