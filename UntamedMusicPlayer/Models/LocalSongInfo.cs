using System.Text.RegularExpressions;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using TagLibSharp2.Core;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.Services;
using ZLinq;
using ZLogger;

namespace UntamedMusicPlayer.Models;

[MemoryPackable]
public partial class BriefLocalSongInfo : IBriefSongInfoBase
{
    protected static readonly string _unknownGenre = "SongInfo_UnknownGenre".GetLocalized();
    protected static readonly ILogger _logger = LoggingService.CreateLogger<BriefLocalSongInfo>();

    /// <summary>
    /// 歌手分隔符
    /// </summary>
    public static char[] Delimiters { get; } = ['、', ',', '，', '|', '/'];

    /// <summary>
    /// 是否可以播放
    /// </summary>
    public bool IsPlayAvailable { get; set; } = true;

    /// <summary>
    /// 文件位置
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// 所处文件夹
    /// </summary>
    public string Folder { get; set; } = null!;

    /// <summary>
    /// 歌曲名
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// 专辑名, 为空时返回"未知专辑"
    /// </summary>
    public virtual string Album { get; set; } = null!;

    /// <summary>
    /// 参与创作的艺术家数组
    /// </summary>
    public string[] Artists
    {
        get;
        set =>
            field = [
                .. value
                    .AsValueEnumerable()
                    .SelectMany(artist =>
                        artist.Split(
                            Delimiters,
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                    )
                    .Distinct(),
            ];
    } = null!;

    /// <summary>
    /// 参与创作的艺术家名, 为空时返回"未知艺术家"
    /// </summary>
    public virtual string ArtistsStr { get; set; } = null!;

    /// <summary>
    /// 时长
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 时长字符串, 为空时返回00:00
    /// </summary>
    public virtual string DurationStr { get; set; } = null!;

    /// <summary>
    /// 发行年份
    /// </summary>
    public ushort Year { get; set; } = 0;

    /// <summary>
    /// 发行年份字符串, 为0时返回""
    /// </summary>
    public string YearStr { get; set; } = null!;

    /// <summary>
    /// 流派数组
    /// </summary>
    public string[] Genre { get; set; } = null!;

    /// <summary>
    /// 流派字符串, 为空时返回"未知流派"
    /// </summary>
    public virtual string GenreStr { get; set; } = null!;

    /// <summary>
    /// 曲目字符串, 为0时返回""
    /// </summary>
    public string TrackStr { get; set; } = null!;

    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate { get; set; } = 0;

    /// <summary>
    /// 是否有封面
    /// </summary>
    public bool HasCover { get; set; } = false;

    [MemoryPackConstructor]
    public BriefLocalSongInfo() { }

    public BriefLocalSongInfo(string path, string folder)
    {
        Path = path;
        Folder = folder;
        ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();

        try
        {
            var result = MediaFile.Read(path);
            if (!result.IsSuccess)
            {
                IsPlayAvailable = false;
                return;
            }
            using var file = (IMediaFile)result.File!;
            Album = result.Tag!.Album ?? IBriefSongInfoBase._unknownAlbum;
            Title = string.IsNullOrEmpty(result.Tag!.Title)
                ? System.IO.Path.GetFileNameWithoutExtension(path)
                : result.Tag!.Title;
            string[] combinedArtists = [.. result.Tag!.AlbumArtists, .. result.Tag!.Performers];
            Artists =
                combinedArtists.Length != 0 ? combinedArtists : [IBriefSongInfoBase._unknownArtist];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(Artists);
            Year = ParseYearFromString(result.Tag!.Year);
            YearStr = IBriefSongInfoBase.GetYearStr(Year);
            var genres = result.Tag!.Genres;
            Genre = genres.Length != 0 ? genres : [_unknownGenre];
            GenreStr = GetGenreStr(Genre);
            TrackStr = result.Tag!.Track == 0 ? "" : $"{result.Tag!.Track}";
            Duration = file.AudioProperties!.Duration;
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            HasCover = result.Tag!.Pictures.Length != 0;
        }
        catch (Exception ex) when (ex is NullReferenceException)
        {
            // 设置默认值
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
            Album = IBriefSongInfoBase._unknownAlbum;
            Artists = [IBriefSongInfoBase._unknownArtist];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(Artists);
            YearStr = "";
            Genre = [_unknownGenre];
            GenreStr = GetGenreStr(Genre);
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
        }
        catch (Exception ex)
        {
            IsPlayAvailable = false;
            _logger.ZLogInformation(ex, $"读取本地音乐文件{Path}信息时发生错误");
        }
    }

    /// <summary>
    /// 获取流派字符串
    /// </summary>
    /// <returns></returns>
    protected static string GetGenreStr(string[] genre) => string.Join(", ", genre);

    private static ushort ParseYearFromString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return 0;
        }

        // 优先匹配开头的数字（常见 "2024" 或 "2024-12-25"）
        var m = ParseBeginNumberRegex().Match(s);
        if (m.Success && int.TryParse(m.Value, out var y) && y >= 0 && y <= ushort.MaxValue)
        {
            return (ushort)y;
        }

        // 尝试解析为完整日期字符串（例如 "2024-12-25"）
        if (DateTime.TryParse(s, out var dt))
        {
            var yy = dt.Year;
            if (yy >= 0 && yy <= ushort.MaxValue)
            {
                return (ushort)yy;
            }
        }

        // 回退：在字符串中查找四位数年份
        m = SearchYearRegex().Match(s);
        if (m.Success && int.TryParse(m.Value, out var y2) && y2 >= 0 && y2 <= ushort.MaxValue)
        {
            return (ushort)y2;
        }

        return 0;
    }

    [GeneratedRegex(@"^\d{1,4}")]
    private static partial Regex ParseBeginNumberRegex();

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex SearchYearRegex();
}

public sealed class DetailedLocalSongInfo : BriefLocalSongInfo, IDetailedSongInfoBase
{
    public bool IsOnline { get; set; } = false;

    /// <summary>
    /// 专辑名, 为空时返回""
    /// </summary>
    public override string Album { get; set; } = "";

    /// <summary>
    /// 参与创作的艺术家名, 为空时返回""
    /// </summary>
    public override string ArtistsStr { get; set; } = "";

    /// <summary>
    /// 流派字符串, 为空时返回""
    /// </summary>
    public override string GenreStr { get; set; } = "";

    /// <summary>
    /// 时长字符串, 为空时返回""
    /// </summary>
    public override string DurationStr { get; set; } = "";

    /// <summary>
    /// 项目类型, 为空时返回""
    /// </summary>
    public string ItemType { get; set; } = "";

    /// <summary>
    /// 专辑艺术家字符串, 为空时返回""
    /// </summary>
    public string AlbumArtistsStr { get; set; } = "";

    /// <summary>
    /// 艺术家和专辑名字符串, 为空时返回""
    /// </summary>
    public string ArtistAndAlbumStr { get; set; } = "";

    /// <summary>
    /// 清晰封面(可能为空)
    /// </summary>
    public BitmapImage? Cover { get; set; }

    /// <summary>
    /// 封面缓冲数据
    /// </summary>
    public byte[]? CoverBuffer { get; set; }

    /// <summary>
    /// 比特率, 为空时返回""
    /// </summary>
    public string BitRate { get; set; } = "";

    /// <summary>
    /// 歌词, 为空时返回""
    /// </summary>
    public string Lyric { get; set; } = "";

    public DetailedLocalSongInfo(BriefLocalSongInfo info)
    {
        IsPlayAvailable = info.IsPlayAvailable;
        Path = info.Path;
        Folder = info.Folder;
        Title = info.Title;
        TrackStr = info.TrackStr;
        try
        {
            var result = MediaFile.Read(Path);
            if (!result.IsSuccess)
            {
                IsPlayAvailable = false;
                return;
            }
            using var file = (IMediaFile)result.File!;
            Album = result.Tag!.Album ?? "";
            Artists = [.. result.Tag!.AlbumArtists, .. result.Tag!.Performers];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(Artists);
            AlbumArtistsStr = IDetailedSongInfoBase.GetAlbumArtistsStr([
                .. result
                    .Tag!.AlbumArtists.AsValueEnumerable()
                    .SelectMany(artist =>
                        artist.Split(
                            Delimiters,
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                    )
                    .Distinct(),
            ]);
            ArtistAndAlbumStr = IDetailedSongInfoBase.GetArtistAndAlbumStr(Album, ArtistsStr);
            Year = info.Year;
            YearStr = info.YearStr;
            Genre = result.Tag!.Genres;
            GenreStr = GetGenreStr(Genre);
            Duration = info.Duration;
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            Lyric = result.Tag!.Lyrics ?? "";
            BitRate = $"{file.AudioProperties!.Bitrate} kbps";
            ModifiedDate = info.ModifiedDate;
            ItemType = System.IO.Path.GetExtension(Path).ToLower();
            if (result.Tag!.Pictures.Length != 0)
            {
                var coverBuffer = result.Tag!.Pictures[0].PictureData.ToArray();
                CoverBuffer = coverBuffer;
                using var stream = new MemoryStream(coverBuffer);
                Cover = new BitmapImage();
                Cover.SetSource(stream.AsRandomAccessStream());
            }
        }
        catch (Exception ex) when (ex is NullReferenceException) { }
        catch (Exception ex) when (ex is FileNotFoundException)
        {
            IsPlayAvailable = false;
        }
        catch (Exception ex)
        {
            IsPlayAvailable = false;
            _logger.ZLogInformation(ex, $"读取本地音乐文件{Path}详细信息时发生错误");
        }
    }
}
