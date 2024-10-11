using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;
public class AlbumInfo
{
    //专辑名
    private readonly string _name = "";
    public string Name => _name;

    //专辑封面路径
    private readonly BitmapImage? _cover;
    public BitmapImage? Cover => _cover;

    private string[] _artists = [];
    /// <summary>
    /// 专辑艺术家
    /// </summary>
    public string[] Artists
    {
        get => _artists;
        set => _artists = value;
    }

    private string _artistsStr = "";
    /// <summary>
    /// 专辑艺术家字符串
    /// </summary>
    public string ArtistsStr
    {
        get => _artistsStr;
        set => _artistsStr = value;
    }

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    public string GetArtistsStr()
    {
        if (_artists == null || _artists.Length == 0)
        {
            return "MusicInfo_UnknownArtist".GetLocalized();
        }
        var sb = new StringBuilder();
        foreach (var artist in _artists)
        {
            sb.Append(artist);
            sb.Append(", ");
        }
        if (sb.Length > 0)
        {
            sb.Length -= 2; // 去掉最后一个逗号
        }
        return sb.ToString();
    }

    private int _totalNum;
    /// <summary>
    /// 专辑包含的歌曲数量
    /// </summary>
    public int TotalNum
    {
        get => _totalNum;
        set => _totalNum = value;
    }

    private TimeSpan _totalDuration;
    /// <summary>
    /// 专辑包含的歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => _totalDuration = value;
    }

    private readonly ushort _year;
    /// <summary>
    /// 专辑发布年份
    /// </summary>
    public ushort Year => _year;

    private readonly long _modifiedDate;
    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate => _modifiedDate;

    private readonly string _genreStr = "";
    /// <summary>
    /// 专辑流派字符串
    /// </summary>
    public string GenreStr => _genreStr;

    public AlbumInfo()
    {
    }
    public AlbumInfo(BriefMusicInfo briefmusicInfo)
    {
        _name = briefmusicInfo.Album;
        _year = briefmusicInfo.Year;
        _modifiedDate = briefmusicInfo.ModifiedDate;
        _cover = briefmusicInfo.Cover;
        Artists = briefmusicInfo.Artists;
        _artistsStr = briefmusicInfo.ArtistsStr;
        _genreStr = briefmusicInfo.GenreStr;
        TotalDuration = briefmusicInfo.Duration;
        TotalNum = 1;
    }

    /// <summary>
    /// 扫描歌曲时更新专辑信息
    /// </summary>
    /// <param name="briefmusicInfo"></param>
    public void Update(BriefMusicInfo briefmusicInfo)
    {
        TotalNum++;
        TotalDuration += briefmusicInfo.Duration;
        Artists = Artists.Concat(briefmusicInfo.Artists).Distinct().ToArray();
        ArtistsStr = GetArtistsStr();
    }

    /// <summary>
    /// 获取专辑的歌曲数量和总时长字符串
    /// </summary>
    /// <returns></returns>
    public string? GetCountAndDurationStr()
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
