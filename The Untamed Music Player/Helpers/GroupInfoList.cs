using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace The_Untamed_Music_Player.Helpers;
public class GroupInfoList(IEnumerable<object> items) : List<object>(items)
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

