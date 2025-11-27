namespace UntamedMusicPlayer.Helpers;

public sealed partial class GroupInfoList(IEnumerable<object> items) : List<object>(items)
{
    public string? Key { get; set; }
}
