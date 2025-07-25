using System.Diagnostics;
using MemoryPack;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;

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

    public BriefLocalSongInfo(string path, string folder)
    {
        Path = path;
        Folder = folder;
        ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();

        try
        {
            var musicFile = TagLib.File.Create(path);
            Album = musicFile.Tag.Album ?? IBriefSongInfoBase._unknownAlbum;
            Title = string.IsNullOrEmpty(musicFile.Tag.Title)
                ? System.IO.Path.GetFileNameWithoutExtension(path)
                : musicFile.Tag.Title;
            string[] combinedArtists = [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers];
            Artists =
                combinedArtists.Length != 0 ? combinedArtists : [IBriefSongInfoBase._unknownArtist];
            ArtistsStr = IBriefSongInfoBase.GetArtistsStr(Artists);
            Year = (ushort)musicFile.Tag.Year;
            YearStr = IBriefSongInfoBase.GetYearStr(Year);
            var genres = musicFile.Tag.Genres;
            Genre = genres.Length != 0 ? genres : [_unknownGenre];
            GenreStr = GetGenreStr(Genre);
            Duration = musicFile.Properties.Duration;
            DurationStr = IBriefSongInfoBase.GetDurationStr(Duration);
            HasCover = musicFile.Tag.Pictures.Length != 0;
        }
        catch (Exception ex)
            when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
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
            Debug.WriteLine(ex.StackTrace);
        }
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
