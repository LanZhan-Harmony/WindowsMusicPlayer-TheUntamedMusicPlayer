using hyjiacan.py4n;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Helpers;
internal class TitleComparer : IComparer<string>
{
    private static readonly PinyinFormat PinyinFormat1 = PinyinFormat.UPPERCASE | PinyinFormat.WITHOUT_TONE | PinyinFormat.WITH_V;
    private static readonly PinyinFormat PinyinFormat2 = PinyinFormat.LOWERCASE | PinyinFormat.WITHOUT_TONE | PinyinFormat.WITH_V;

    public static string GetPinyinFirstLetter(char text)
    {
        return Pinyin4Net.GetFirstPinyin(text, PinyinFormat1);
    }

    public static string GetPinyin(string text)
    {
        return Pinyin4Net.GetPinyin(text, PinyinFormat2);
    }

    public int Compare(string? x, string? y)
    {
        if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(x))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(y))
        {
            return 1;
        }

        var xFirstChar = x[0];
        var yFirstChar = y[0];

        var xGroupKey = GetGroupKey(xFirstChar);
        var yGroupKey = GetGroupKey(yFirstChar);

        var xPriority = GetGroupPriority(xGroupKey);
        var yPriority = GetGroupPriority(yGroupKey);

        // 先按分组优先级排序
        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }

        // 如果分组优先级相同，则按分组键排序
        var groupComparison = string.Compare(xGroupKey, yGroupKey, StringComparison.OrdinalIgnoreCase);
        if (groupComparison != 0)
        {
            return groupComparison;
        }

        // 如果分组键相同，则按实际内容排序
        if (IsChinese(xFirstChar))
        {
            var xPinyin = GetPinyin(x);
            var yPinyin = GetPinyin(y);
            return string.Compare(xPinyin, yPinyin, StringComparison.OrdinalIgnoreCase);
        }

        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetGroupKey(char firstChar)
    {
        if (IsChinese(firstChar))
        {
            var prefix = "LocalSongs_Pinyin".GetLocalized();
            var pinyin = GetPinyinFirstLetter(firstChar);
            if (prefix == "拼音")
            {
                return $"拼音{pinyin[0]}";
            }
            else
            {
                return prefix;
            }
        }
        else if (char.IsLetter(firstChar))
        {
            return char.ToUpper(firstChar).ToString();
        }
        else if (char.IsDigit(firstChar))
        {
            return "#";
        }
        else
        {
            return "&";
        }
    }

    public static bool IsChinese(char c)
    {
        return c >= 0x4E00 && c <= 0x9FFF;
    }

    private static int GetGroupPriority(string groupKey)
    {
        return groupKey switch
        {
            "&" => 1,
            "#" => 2,
            _ when groupKey.StartsWith("拼音") || groupKey.StartsWith("...") => 4,
            _ => 3, // 字母
        };
    }
}

internal abstract class BaseArtistComparer<T> : IComparer<T>
{
    public abstract int Compare(T? x, T? y);

    protected static int CompareByProperty(string? xProperty, string? yProperty, string? xTitle, string? yTitle)
    {
        if (string.IsNullOrEmpty(xProperty) && string.IsNullOrEmpty(yProperty))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(xProperty))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(yProperty))
        {
            return 1;
        }

        var xFirstChar = xProperty[0];
        var xPriority = GetGroupPriority(xProperty);
        var yPriority = GetGroupPriority(yProperty);

        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }

        var groupComparison = TitleComparer.IsChinese(xFirstChar)
            ? string.Compare(TitleComparer.GetPinyin(xProperty), TitleComparer.GetPinyin(yProperty), StringComparison.OrdinalIgnoreCase)
            : string.Compare(xProperty, yProperty, StringComparison.OrdinalIgnoreCase);

        if (groupComparison != 0)
        {
            return groupComparison;
        }

        return new TitleComparer().Compare(xTitle, yTitle);
    }

    protected static int GetGroupPriority(string property)
    {
        if (property == "MusicInfo_UnknownArtist".GetLocalized())
        {
            return 0;
        }
        else if (TitleComparer.IsChinese(property[0]))
        {
            return 4;
        }
        else if (char.IsLetter(property[0]))
        {
            return 3;
        }
        else if (char.IsDigit(property[0]))
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}

internal class MusicArtistComparer : BaseArtistComparer<BriefMusicInfo>
{
    public override int Compare(BriefMusicInfo? x, BriefMusicInfo? y)
    {
        return CompareByProperty(x?.ArtistsStr, y?.ArtistsStr, x?.Title, y?.Title);
    }
}

internal class AlbumArtistComparer : BaseArtistComparer<AlbumInfo>
{
    public override int Compare(AlbumInfo? x, AlbumInfo? y)
    {
        return CompareByProperty(x?.ArtistsStr, y?.ArtistsStr, x?.Name, y?.Name);
    }
}

internal class ArtistTitleComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        return CompareByProperty(x, y);
    }

    private static int CompareByProperty(string? xProperty, string? yProperty)
    {
        if (string.IsNullOrEmpty(xProperty) && string.IsNullOrEmpty(yProperty))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(xProperty))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(yProperty))
        {
            return 1;
        }
        var xFirstChar = xProperty[0];
        var xPriority = GetGroupPriority(xProperty);
        var yPriority = GetGroupPriority(yProperty);
        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }
        var groupComparison = TitleComparer.IsChinese(xFirstChar)
            ? string.Compare(TitleComparer.GetPinyin(xProperty), TitleComparer.GetPinyin(yProperty), StringComparison.OrdinalIgnoreCase)
            : string.Compare(xProperty, yProperty, StringComparison.OrdinalIgnoreCase);

        return groupComparison;
    }

    protected static int GetGroupPriority(string property)
    {
        if (property == "MusicInfo_UnknownArtist".GetLocalized())
        {
            return 0;
        }
        else if (TitleComparer.IsChinese(property[0]))
        {
            return 4;
        }
        else if (char.IsLetter(property[0]))
        {
            return 3;
        }
        else if (char.IsDigit(property[0]))
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}

internal class MusicAlbumComparer : IComparer<BriefMusicInfo>
{
    public int Compare(BriefMusicInfo? x, BriefMusicInfo? y)
    {
        return CompareByProperty(x?.Album, y?.Album, x?.Title, y?.Title);
    }

    private static int CompareByProperty(string? xProperty, string? yProperty, string? xTitle, string? yTitle)
    {
        if (string.IsNullOrEmpty(xProperty) && string.IsNullOrEmpty(yProperty))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(xProperty))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(yProperty))
        {
            return 1;
        }

        var xFirstChar = xProperty[0];

        var xPriority = GetGroupPriority(xProperty);
        var yPriority = GetGroupPriority(yProperty);

        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }

        var groupComparison = TitleComparer.IsChinese(xFirstChar)
            ? string.Compare(TitleComparer.GetPinyin(xProperty), TitleComparer.GetPinyin(yProperty), StringComparison.OrdinalIgnoreCase)
            : string.Compare(xProperty, yProperty, StringComparison.OrdinalIgnoreCase);

        if (groupComparison != 0)
        {
            return groupComparison;
        }

        return new TitleComparer().Compare(xTitle, yTitle);
    }

    private static int GetGroupPriority(string property)
    {
        if (property == "MusicInfo_UnknownAlbum".GetLocalized())
        {
            return 0;
        }
        else if (TitleComparer.IsChinese(property[0]))
        {
            return 4;
        }
        else if (char.IsLetter(property[0]))
        {
            return 3;
        }
        else if (char.IsDigit(property[0]))
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}

internal class AlbumTitleComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        return CompareByProperty(x, y);
    }

    private static int CompareByProperty(string? xProperty, string? yProperty)
    {
        if (string.IsNullOrEmpty(xProperty) && string.IsNullOrEmpty(yProperty))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(xProperty))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(yProperty))
        {
            return 1;
        }
        var xFirstChar = xProperty[0];
        var xPriority = GetGroupPriority(xProperty);
        var yPriority = GetGroupPriority(yProperty);
        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }
        var groupComparison = TitleComparer.IsChinese(xFirstChar)
            ? string.Compare(TitleComparer.GetPinyin(xProperty), TitleComparer.GetPinyin(yProperty), StringComparison.OrdinalIgnoreCase)
            : string.Compare(xProperty, yProperty, StringComparison.OrdinalIgnoreCase);
        return groupComparison;
    }

    private static int GetGroupPriority(string property)
    {
        if (property == "MusicInfo_UnknownAlbum".GetLocalized())
        {
            return 0;
        }
        else if (TitleComparer.IsChinese(property[0]))
        {
            return 4;
        }
        else if (char.IsLetter(property[0]))
        {
            return 3;
        }
        else if (char.IsDigit(property[0]))
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}

internal class MusicFolderComparer : IComparer<BriefMusicInfo>
{
    public int Compare(BriefMusicInfo? x, BriefMusicInfo? y)
    {
        return CompareByProperty(x?.Folder, y?.Folder, x?.Title, y?.Title);
    }

    private static int CompareByProperty(string? xProperty, string? yProperty, string? xTitle, string? yTitle)
    {
        if (string.IsNullOrEmpty(xProperty) && string.IsNullOrEmpty(yProperty))
        {
            return 0;
        }
        else if (string.IsNullOrEmpty(xProperty))
        {
            return -1;
        }
        else if (string.IsNullOrEmpty(yProperty))
        {
            return 1;
        }

        var groupComparison = new TitleComparer().Compare(xProperty, yProperty);
        if (groupComparison != 0)
        {
            return groupComparison;
        }
        return new TitleComparer().Compare(xTitle, yTitle);
    }
}

internal class GenreComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y))
        {
            return 0;
        }
        if (string.IsNullOrEmpty(x))
        {
            return -1;
        }
        if (string.IsNullOrEmpty(y))
        {
            return 1;
        }

        var xFirstChar = x[0];

        var xPriority = GetGroupPriority(x);
        var yPriority = GetGroupPriority(y);

        if (xPriority != yPriority)
        {
            return xPriority.CompareTo(yPriority);
        }

        var groupComparison = TitleComparer.IsChinese(xFirstChar)
            ? string.Compare(TitleComparer.GetPinyin(x), TitleComparer.GetPinyin(y), StringComparison.OrdinalIgnoreCase)
            : string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        return groupComparison;
    }

    private static int GetGroupPriority(string property)
    {
        if (property == "MusicInfo_AllGenres".GetLocalized())
        {
            return 0;
        }
        else if (property == "MusicInfo_UnknownGenre".GetLocalized())
        {
            return 1;
        }
        else if (TitleComparer.IsChinese(property[0]))
        {
            return 5;
        }
        else if (char.IsLetter(property[0]))
        {
            return 4;
        }
        else if (char.IsDigit(property[0]))
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }
}