using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;
public class BriefAlbumInfo(AlbumInfo albumInfo)
{
    protected static readonly string _unknownYear = "AlbumInfo_UnknownYear".GetLocalized();

    public string Name { get; set; } = albumInfo.Name;
    public string YearStr { get; set; } = albumInfo.Year == 0 ? _unknownYear : albumInfo.Year.ToString();
    public BitmapImage? Cover { get; set; } = albumInfo.Cover;
    public List<IBriefMusicInfoBase> SongList { get; set; } = [.. Data.MusicLibrary.GetSongsByAlbum(albumInfo)];
}

public class AlbumInfo
{
    /// <summary>
    /// 专辑名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 专辑封面
    /// </summary>
    public BitmapImage? Cover { get; set; }

    /// <summary>
    /// 专辑封面来源歌曲的路径
    /// </summary>
    public string? CoverPath { get; set; }

    /// <summary>
    /// 专辑艺术家
    /// </summary>
    public string[] Artists { get; set; }

    /// <summary>
    /// 专辑艺术家字符串
    /// </summary>
    public string ArtistsStr { get; set; }

    /// <summary>
    /// 专辑包含的歌曲数量
    /// </summary>
    public int TotalNum { get; set; } = 1;

    /// <summary>
    /// 专辑包含的歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 专辑发布年份
    /// </summary>
    public ushort Year { get; set; }

    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate { get; set; }

    /// <summary>
    /// 专辑流派字符串
    /// </summary>
    public string GenreStr { get; set; }

    public AlbumInfo(BriefMusicInfo briefmusicInfo)
    {
        Name = briefmusicInfo.Album;
        Year = briefmusicInfo.Year;
        ModifiedDate = briefmusicInfo.ModifiedDate;
        if (briefmusicInfo.Cover is not null)
        {
            Cover = briefmusicInfo.Cover;
            CoverPath = briefmusicInfo.Path;
        }
        Artists = briefmusicInfo.Artists;
        ArtistsStr = briefmusicInfo.ArtistsStr;
        GenreStr = briefmusicInfo.GenreStr;
        TotalDuration = briefmusicInfo.Duration;
    }

    /// <summary>
    /// 扫描歌曲时更新专辑信息
    /// </summary>
    /// <param name="briefmusicInfo"></param>
    public void Update(BriefMusicInfo briefmusicInfo)
    {
        TotalNum++;
        TotalDuration += briefmusicInfo.Duration;
        if (Cover is null && briefmusicInfo.Cover is not null)
        {
            Cover = briefmusicInfo.Cover;
            CoverPath = briefmusicInfo.Path;
        }
        Artists = [.. Artists!.Concat(briefmusicInfo.Artists).Distinct()];
        ArtistsStr = GetArtistsStr();
    }

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    public string GetArtistsStr() => string.Join(", ", Artists!);

    public byte[] GetCoverBytes()
    {
        if (Cover is not null)
        {
            try
            {
                var musicFile = TagLib.File.Create(CoverPath);
                return musicFile.Tag.Pictures[0].Data.Data;
            }
            catch { }
        }
        return [];
    }

    /// <summary>
    /// 获取专辑的简介字符串
    /// </summary>
    /// <returns></returns>
    public string GetDescriptionStr()
    {
        var parts = new List<string>();
        if (Year != 0)
        {
            parts.Add(Year.ToString());
        }
        if (GenreStr != "")
        {
            parts.Add(GenreStr);
        }
        parts.Add(TotalNum > 1
            ? $"{TotalNum} {"AlbumInfo_Songs".GetLocalized()}"
            : $"{TotalNum} {"AlbumInfo_Song".GetLocalized()}");
        parts.Add(TotalDuration.Hours > 0
            ? $"{TotalDuration:hh\\:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
            : $"{TotalDuration:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}");
        return string.Join(" • ", parts);
    }
}
