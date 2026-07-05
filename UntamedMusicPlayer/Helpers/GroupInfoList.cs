using Microsoft.UI.Xaml;

namespace UntamedMusicPlayer.Helpers;

public sealed partial class GroupInfoList(IEnumerable<object> items) : List<object>(items)
{
    public string? Key { get; set; }

    public double ZoomedOutViewGridWidth { get; set; } = 71;

    public Thickness ZoomedOutViewTextBlockMargin { get; set; } = new(0);
}
