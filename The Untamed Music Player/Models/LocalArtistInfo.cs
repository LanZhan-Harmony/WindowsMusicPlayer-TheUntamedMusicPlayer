using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;

[MemoryPackable]
public partial class LocalArtistInfo : IArtistInfoBase
{
    public HashSet<string> Albums { get; set; } = [];

    /// <summary>
    /// 艺术家名
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 艺术家流派
    /// </summary>
    public string GenreStr { get; set; } = null!;

    /// <summary>
    /// 艺术家封面
    /// </summary>
    [MemoryPackIgnore]
    public BitmapImage? Cover { get; set; }

    /// <summary>
    /// 专辑封面来源歌曲的路径
    /// </summary>
    public string? CoverPath { get; set; }

    /// <summary>
    /// 艺术家歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 艺术家歌曲总数
    /// </summary>
    public int TotalSongNum { get; set; } = 1;

    /// <summary>
    /// 艺术家专辑总数
    /// </summary>
    public int TotalAlbumNum { get; set; } = 1;

    [MemoryPackConstructor]
    public LocalArtistInfo() { }

    public LocalArtistInfo(BriefLocalSongInfo briefLocalSongInfo, string name)
    {
        Name = name;
        TotalDuration = briefLocalSongInfo.Duration;
        GenreStr = briefLocalSongInfo.GenreStr;
        Albums.Add(briefLocalSongInfo.Album);
    }

    /// <summary>
    /// 扫描歌曲时更新艺术家信息
    /// </summary>
    /// <param name="briefLocalSongInfo"></param>
    public void Update(BriefLocalSongInfo briefLocalSongInfo)
    {
        TotalDuration += briefLocalSongInfo.Duration;
        TotalSongNum++;
        var album = briefLocalSongInfo.Album;

        if (Albums.Add(album))
        {
            TotalAlbumNum++;
        }
    }

    public byte[] GetCoverBytes()
    {
        if (Cover is not null)
        {
            var musicFile = TagLib.File.Create(CoverPath);
            return musicFile.Tag.Pictures[0].Data.Data;
        }
        return [];
    }

    /// <summary>
    /// 获取专辑数量和歌曲数量
    /// </summary>
    /// <returns></returns>
    public string GetCountStr()
    {
        var albumStr =
            TotalAlbumNum > 1
                ? "ArtistInfo_Albums".GetLocalized()
                : "ArtistInfo_Album".GetLocalized();
        var songStr =
            TotalSongNum > 1 ? "AlbumInfo_Songs".GetLocalized() : "AlbumInfo_Song".GetLocalized();
        return $"{TotalAlbumNum} {albumStr} • {TotalSongNum} {songStr} •";
    }

    /// <summary>
    /// 获取总时长
    /// </summary>
    /// <returns></returns>
    public string GetDurationStr()
    {
        var hourStr =
            TotalDuration.Hours > 1
                ? "ArtistInfo_Hours".GetLocalized()
                : "ArtistInfo_Hour".GetLocalized();
        var minuteStr =
            TotalDuration.Minutes > 1
                ? "ArtistInfo_Mins".GetLocalized()
                : "ArtistInfo_Min".GetLocalized();
        var secondStr =
            TotalDuration.Seconds > 1
                ? "ArtistInfo_Secs".GetLocalized()
                : "ArtistInfo_Sec".GetLocalized();

        return TotalDuration.Hours > 0
            ? $"{TotalDuration.Hours} {hourStr} {TotalDuration.Minutes} {minuteStr} {TotalDuration.Seconds} {secondStr}"
            : $"{TotalDuration.Minutes} {minuteStr} {TotalDuration.Seconds} {secondStr}";
    }

    public string GetDescriptionStr() => $"{"ArtistInfo_Artist".GetLocalized()} • {GenreStr}";
}
