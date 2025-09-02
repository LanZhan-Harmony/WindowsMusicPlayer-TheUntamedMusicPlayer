namespace The_Untamed_Music_Player.Helpers;

public partial class GroupInfoList(IEnumerable<object> items) : List<object>(items)
{
    public string? Key { get; set; }
}
