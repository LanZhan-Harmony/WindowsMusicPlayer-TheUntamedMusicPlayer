namespace UntamedMusicPlayer.Core.Lyrics;

/// <summary>
/// A parsed lyric segment independent of any presentation framework.
/// </summary>
public sealed class LyricSlice(double startTime, string content)
{
    public string Content { get; set; } = content;

    /// <summary>
    /// Segment start time in milliseconds.
    /// </summary>
    public double StartTime { get; set; } = startTime;

    /// <summary>
    /// Segment end time in milliseconds.
    /// </summary>
    public double EndTime { get; set; }
}
