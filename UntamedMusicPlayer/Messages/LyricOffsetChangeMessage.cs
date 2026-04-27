namespace UntamedMusicPlayer.Messages;

public sealed class LyricOffsetChangeMessage(int offsetMilliseconds)
{
    public int OffsetMilliseconds { get; } = offsetMilliseconds;
}
