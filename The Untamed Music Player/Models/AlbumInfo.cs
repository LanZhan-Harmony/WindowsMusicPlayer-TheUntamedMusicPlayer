using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class AlbumInfo
{
    //专辑名
    private string _name = "";
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    //专辑封面路径
    private BitmapImage? _cover;
    public BitmapImage? Cover
    {
        get => _cover;
        set => _cover = value;
    }

    private string[] _artists = [];
    public string[] Artists
    {
        get => _artists;
        set => _artists = value;
    }

    private string _artistsStr = "";
    public string ArtistsStr
    {
        get => _artistsStr;
        set => _artistsStr = value;
    }

    //专辑包含的歌曲数量
    private int _totalNum;
    public int TotalNum
    {
        get => _totalNum;
        set => _totalNum = value;
    }

    private TimeSpan _totalDuration;
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => _totalDuration = value;
    }

    private ushort _year;
    //专辑发布年份
    public ushort Year
    {
        get => _year;
        set => _year = value;
    }

    private long _modifiedDate;
    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate
    {
        get => _modifiedDate;
        set => _modifiedDate = value;
    }

    private string _genreStr = "";
    public string GenreStr
    {
        get => _genreStr;
        set => _genreStr = value;
    }

    public AlbumInfo()
    {
    }
    public AlbumInfo(BriefMusicInfo briefmusicInfo)
    {
        Name = briefmusicInfo.Album;
        Year = briefmusicInfo.Year;
        ModifiedDate = briefmusicInfo.ModifiedDate;
        Cover = briefmusicInfo.Cover;
        Artists = briefmusicInfo.Artists;
        ArtistsStr = briefmusicInfo.ArtistsStr;
        GenreStr = briefmusicInfo.GenreStr;
        TotalDuration = briefmusicInfo.Duration;
        TotalNum = 1;
    }

    public void Update(BriefMusicInfo briefmusicInfo)
    {
        TotalNum++;
        TotalDuration += briefmusicInfo.Duration;
    }

    public string? GetCountAndDuration()
    {
        var yearStr = Year == 0 ? "" : Year.ToString();
        if (TotalDuration.Hours > 0)
        {
            return string.IsNullOrEmpty(yearStr)
                ? $"{TotalNum} 首歌曲·{TotalDuration:hh\\:mm\\:ss} 歌曲长度"
                : $"{yearStr}·{TotalNum} 首歌曲·{TotalDuration:hh\\:mm\\:ss} 歌曲长度";
        }
        else
        {
            return string.IsNullOrEmpty(yearStr)
                ? $"{TotalNum} 首歌曲·{TotalDuration:mm\\:ss} 歌曲长度"
                : $"{yearStr}·{TotalNum} 首歌曲·{TotalDuration:mm\\:ss} 歌曲长度";
        }
    }
}
