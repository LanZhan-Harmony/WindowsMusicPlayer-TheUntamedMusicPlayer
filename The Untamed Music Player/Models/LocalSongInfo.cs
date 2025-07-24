using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Models;

[MemoryPackable]
public partial class BriefLocalSongInfo : IBriefSongInfoBase
{
    /// <summary>
    /// 歌手分隔符
    /// </summary>
    protected static readonly char[] _delimiters = ['、', ',', '，', '|', '/'];
    protected static readonly string _unknownGenre = "SongInfo_UnknownGenre".GetLocalized();

    /// <summary>
    /// 是否可以播放
    /// </summary>
    public bool IsPlayAvailable { get; set; } = true;

    /// <summary>
    /// 在播放队列中的索引
    /// </summary>
    public int PlayQueueIndex { get; set; } = -1;

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
                    .SelectMany(artist =>
                        artist.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)
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
    /// 修改日期
    /// </summary>
    public long ModifiedDate { get; set; } = 0;

    /// <summary>
    /// 是否有封面
    /// </summary>
    public bool HasCover { get; set; } = false;

    [MemoryPackConstructor]
    public BriefLocalSongInfo() { }

    /// <summary>
    /// 异步工厂方法
    /// </summary>
    /// <param name="path"></param>
    /// <param name="folder"></param>
    /// <returns></returns>
    public static BriefLocalSongInfo Create(string path, string folder)
    {
        var info = new BriefLocalSongInfo
        {
            Path = path,
            Folder = folder,
            ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds(),
        };

        try
        {
            var musicFile = TagLib.File.Create(path);
            info.Album = musicFile.Tag.Album ?? IBriefSongInfoBase._unknownAlbum;
            info.Title = string.IsNullOrEmpty(musicFile.Tag.Title)
                ? System.IO.Path.GetFileNameWithoutExtension(path)
                : musicFile.Tag.Title;
            string[] combinedArtists = [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers];
            info.Artists =
                combinedArtists.Length != 0 ? combinedArtists : [IBriefSongInfoBase._unknownArtist];
            info.ArtistsStr = IBriefSongInfoBase.GetArtistsStr(info.Artists);
            info.Year = (ushort)musicFile.Tag.Year;
            info.YearStr = IBriefSongInfoBase.GetYearStr(info.Year);
            var genres = musicFile.Tag.Genres;
            info.Genre = genres.Length != 0 ? genres : [_unknownGenre];
            info.GenreStr = GetGenreStr(info.Genre);
            info.Duration = musicFile.Properties.Duration;
            info.DurationStr = IBriefSongInfoBase.GetDurationStr(info.Duration);
            info.HasCover = musicFile.Tag.Pictures.Length != 0;
        }
        catch (Exception ex)
            when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
        {
            // 设置默认值
            info.Title = System.IO.Path.GetFileNameWithoutExtension(path);
            info.Album = IBriefSongInfoBase._unknownAlbum;
            info.Artists = [IBriefSongInfoBase._unknownArtist];
            info.ArtistsStr = IBriefSongInfoBase.GetArtistsStr(info.Artists);
            info.YearStr = "";
            info.Genre = [_unknownGenre];
            info.GenreStr = GetGenreStr(info.Genre);
            info.DurationStr = IBriefSongInfoBase.GetDurationStr(info.Duration);
        }
        catch (Exception ex)
        {
            info.IsPlayAvailable = false;
            Debug.WriteLine(ex.StackTrace);
        }
        return info;
    }

    /// <summary>
    /// 获取流派字符串
    /// </summary>
    /// <returns></returns>
    protected static string GetGenreStr(string[] genre) => string.Join(", ", genre);

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public class DetailedLocalSongInfo : BriefLocalSongInfo, IDetailedSongInfoBase
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
    /// 曲目, 为空时返回""
    /// </summary>
    public string Track { get; set; } = "";

    /// <summary>
    /// 歌词, 为空时返回""
    /// </summary>
    public string Lyric { get; set; } = "";

    public DetailedLocalSongInfo(BriefLocalSongInfo info)
    {
        try
        {
            IsPlayAvailable = info.IsPlayAvailable;
            Path = info.Path;
            Folder = info.Folder;
            Title = info.Title;
            var musicFile = TagLib.File.Create(Path);
            Album = musicFile.Tag.Album ?? "";
            Artists = [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(Artists);
            AlbumArtistsStr = IDetailedSongInfoBase.GetAlbumArtistsStr(
                [
                    .. musicFile
                        .Tag.AlbumArtists.SelectMany(artist =>
                            artist.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)
                        )
                        .Distinct(),
                ]
            );
            ArtistAndAlbumStr = IDetailedSongInfoBase.GetArtistAndAlbumStr(Album, ArtistsStr);
            Year = info.Year;
            YearStr = info.YearStr;
            Genre = musicFile.Tag.Genres;
            GenreStr = GetGenreStr(Genre);
            Duration = info.Duration;
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            Track = musicFile.Tag.Track == 0 ? "" : $"{musicFile.Tag.Track}";
            Lyric = musicFile.Tag.Lyrics ?? "";
            BitRate = $"{musicFile.Properties.AudioBitrate} kbps";
            ModifiedDate = info.ModifiedDate;
            ItemType = System.IO.Path.GetExtension(Path).ToLower();
            if (musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                CoverBuffer = coverBuffer;
                using var stream = new MemoryStream(coverBuffer);
                Cover = new BitmapImage { DecodePixelWidth = 400 };
                Cover.SetSource(stream.AsRandomAccessStream());
            }
        }
        catch (Exception ex)
            when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException) { }
        catch (Exception ex) when (ex is FileNotFoundException)
        {
            IsPlayAvailable = false;
        }
        catch (Exception ex)
        {
            IsPlayAvailable = false;
            Debug.WriteLine(ex.StackTrace);
        }
    }
}
