using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;
public class AlbumInfo
{
    /// <summary>
    /// 专辑名
    /// </summary>
    public string Name
    {
        get; set;
    } = "";

    /// <summary>
    /// 专辑封面
    /// </summary>

    public BitmapImage? Cover
    {
        get; set;
    }

    /// <summary>
    /// 专辑艺术家
    /// </summary>
    public string[] Artists
    {
        get; set;
    } = [];

    /// <summary>
    /// 专辑艺术家字符串
    /// </summary>
    public string ArtistsStr
    {
        get;
        set;
    } = "";

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    public string GetArtistsStr()
    {
        if (Artists == null || Artists.Length == 0)
        {
            return "MusicInfo_UnknownArtist".GetLocalized();
        }
        var sb = new StringBuilder();
        foreach (var artist in Artists)
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

    /// <summary>
    /// 专辑包含的歌曲数量
    /// </summary>
    public int TotalNum
    {
        get; set;
    }

    /// <summary>
    /// 专辑包含的歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration
    {
        get; set;
    }

    /// <summary>
    /// 专辑发布年份
    /// </summary>
    public ushort Year
    {
        get; set;
    }

    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate
    {
        get; set;
    }

    /// <summary>
    /// 专辑流派字符串
    /// </summary>
    public string GenreStr
    {
        get; set;
    } = "";

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
