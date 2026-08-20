namespace UntamedMusicPlayer.Core.Contracts.Services;

/// <summary>
/// Schedules playback state notifications on the presentation thread.
/// </summary>
public interface IPlaybackDispatcher
{
    bool TryEnqueue(Action action, bool highPriority = false);
}
