namespace The_Untamed_Music_Player.Helpers;

public partial class GroupInfoList(IEnumerable<object> items) : List<object>(items)
{
    public object? Key
    {
        get; set;
    }

    public override string ToString()
    {
        return "Group " + Key?.ToString();
    }
}

