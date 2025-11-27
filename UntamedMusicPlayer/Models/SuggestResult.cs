namespace UntamedMusicPlayer.Models;

public sealed class SuggestResult
{
    public string Icon { get; set; } = null!;
    public string Label { get; set; } = null!;

    public override string ToString()
    {
        return Label;
    }
}
