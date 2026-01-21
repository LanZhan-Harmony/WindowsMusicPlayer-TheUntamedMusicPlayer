using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Services;
using ZLinq;

namespace UntamedMusicPlayer.Models;

[MemoryPackable]
public sealed partial class LocalAlbumInfo : IAlbumInfoBase
{
    /// <summary>
    /// 专辑名
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 专辑封面来源歌曲的路径
    /// </summary>
    public string? CoverPath { get; set; }

    /// <summary>
    /// 专辑艺术家
    /// </summary>
    public string[] Artists { get; set; } = null!;

    /// <summary>
    /// 专辑艺术家字符串
    /// </summary>
    public string ArtistsStr { get; set; } = null!;

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
    public string GenreStr { get; set; } = null!;

    [MemoryPackConstructor]
    public LocalAlbumInfo() { }

    public LocalAlbumInfo(BriefLocalSongInfo briefLocalSongInfo)
    {
        Name = briefLocalSongInfo.Album;
        Year = briefLocalSongInfo.Year;
        ModifiedDate = briefLocalSongInfo.ModifiedDate;
        if (briefLocalSongInfo.HasCover)
        {
            CoverPath = briefLocalSongInfo.Path;
        }
        Artists = briefLocalSongInfo.Artists;
        ArtistsStr = briefLocalSongInfo.ArtistsStr;
        GenreStr = briefLocalSongInfo.GenreStr;
        TotalDuration = briefLocalSongInfo.Duration;
    }

    /// <summary>
    /// 扫描歌曲时更新专辑信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    public void Update(BriefLocalSongInfo briefLocalSongInfo)
    {
        TotalNum++;
        TotalDuration += briefLocalSongInfo.Duration;
        if (CoverPath is null && briefLocalSongInfo.HasCover)
        {
            CoverPath = briefLocalSongInfo.Path;
        }
        Artists = [.. Artists.AsValueEnumerable().Concat(briefLocalSongInfo.Artists).Distinct()];
        ArtistsStr = IAlbumInfoBase.GetArtistsStr(Artists);
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
            parts.Add($"{Year}");
        }
        if (GenreStr != "")
        {
            parts.Add(GenreStr);
        }
        parts.Add(
            TotalNum == 1
                ? $"{TotalNum} {"AlbumInfo_Song".GetLocalized()}"
                : $"{TotalNum} {"AlbumInfo_Songs".GetLocalized()}"
        );
        parts.Add(
            TotalDuration.Hours > 0
                ? $"{TotalDuration:hh\\:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
                : $"{TotalDuration:mm\\:ss} {"AlbumInfo_RunTime".GetLocalized()}"
        );
        return string.Join(" • ", parts);
    }
}

public class LocalArtistAlbumInfo(LocalAlbumInfo localAlbumInfo) : IArtistAlbumInfoBase
{
    public string Name { get; set; } = localAlbumInfo.Name;
    public string YearStr { get; set; } = IArtistAlbumInfoBase.GetYearStr(localAlbumInfo.Year);
    public BitmapImage? Cover { get; set; } = CoverManager.GetAlbumCoverBitmap(localAlbumInfo);
    public List<IBriefSongInfoBase> SongList { get; set; } =
    [.. Data.MusicLibrary.GetSongsByAlbum(localAlbumInfo)];
}
