using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Models;
public class BriefMusicInfo
{
    /// <summary>
    /// 歌手分隔符
    /// </summary>
    protected static readonly char[] _delimiters = ['、', ',', '，', '|', '/'];

    /// <summary>
    /// 文件位置
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// 所处文件夹
    /// </summary>
    public string Folder { get; set; } = "";

    /// <summary>
    /// 项目类型
    /// </summary>
    public string ItemType { get; set; } = "";

    /// <summary>
    /// 专辑名, 为空时返回"未知专辑"
    /// </summary>
    public virtual string Album { get; set; } = "";

    /// <summary>
    /// 歌曲名
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 参与创作的艺术家数组
    /// </summary>
    public string[] Artists
    {
        get;
        set => field = [.. value
                    .SelectMany(artist => artist.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries))
                    .Distinct()];
    } = [];

    /// <summary>
    /// 参与创作的艺术家名, 为空时返回"未知艺术家"
    /// </summary>
    public virtual string ArtistsStr { get; set; } = "";

    /// <summary>
    /// 时长
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 时长字符串
    /// </summary>
    public virtual string DurationStr { get; set; } = "";

    /// <summary>
    /// 发行年份
    /// </summary>
    public ushort Year { get; set; } = 0;

    /// <summary>
    /// 发行年份字符串, 为0时返回空字符串
    /// </summary>
    public string YearStr { get; set; } = "";

    /// <summary>
    /// 封面(可能为空)
    /// </summary>
    public virtual BitmapImage? Cover
    {
        get; set;
    }

    /// <summary>
    /// 流派数组
    /// </summary>
    public string[] Genre { get; set; } = [];

    /// <summary>
    /// 流派字符串, 为空时返回"未知流派"
    /// </summary>
    public virtual string GenreStr { get; set; } = "";

    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate { get; set; } = 0;

    public BriefMusicInfo()
    {
    }

    /// <summary>
    /// 异步工厂方法
    /// </summary>
    /// <param name="path"></param>
    /// <param name="folder"></param>
    /// <returns></returns>
    public static async Task<BriefMusicInfo> CreateAsync(string path, string folder)
    {
        var info = new BriefMusicInfo
        {
            Path = path,
            Folder = folder,
            ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds(),
            ItemType = System.IO.Path.GetExtension(path).ToLower()
        };

        Task? coverTask = null;
        try
        {
            var musicFile = TagLib.File.Create(path);
            if (musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                coverTask = info.LoadCoverAsync(coverBuffer);
            }
            info.Album = musicFile.Tag.Album ?? "MusicInfo_UnknownAlbum".GetLocalized();
            info.Title = string.IsNullOrEmpty(musicFile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(path) : musicFile.Tag.Title;
            string[] combinedArtists = [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers];
            info.Artists = combinedArtists.Length != 0 ? combinedArtists : ["MusicInfo_UnknownArtist".GetLocalized()];
            info.ArtistsStr = info.GetArtistsStr();
            info.Year = (ushort)musicFile.Tag.Year;
            info.YearStr = info.GetYearStr();
            var genres = musicFile.Tag.Genres;
            info.Genre = genres.Length != 0 ? genres : ["MusicInfo_UnknownGenre".GetLocalized()];
            info.GenreStr = info.GetGenreStr();
            info.Duration = musicFile.Properties.Duration;
            info.DurationStr = info.GetDurationStr();

            // 等待 LoadCoverAsync 任务完成
            if (coverTask != null)
            {
                await coverTask;
            }
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
        {
            // 设置默认值
            info.Title = System.IO.Path.GetFileNameWithoutExtension(path);
            info.Album = "MusicInfo_UnknownAlbum".GetLocalized();
            info.Artists = ["MusicInfo_UnknownArtist".GetLocalized()];
            info.ArtistsStr = info.GetArtistsStr();
            info.Genre = ["MusicInfo_UnknownGenre".GetLocalized()];
            info.GenreStr = info.GetGenreStr();
            info.DurationStr = info.GetDurationStr();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
        return info;
    }

    /// <summary>
    /// 异步加载封面方法
    /// </summary>
    /// <param name="coverBuffer"></param>
    /// <returns></returns>
    private Task<bool> LoadCoverAsync(byte[] coverBuffer)
    {
        var tcs = new TaskCompletionSource<bool>();
        App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(coverBuffer.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage
                {
                    DecodePixelWidth = 160,
                    DecodePixelHeight = 160
                };
                await bitmap.SetSourceAsync(stream);
                Cover = bitmap;
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// 获取参与创作的艺术家名字符串, 为空时返回"未知艺术家"
    /// </summary>
    /// <returns></returns>
    protected virtual string GetArtistsStr() => string.Join(", ", Artists);

    /// <summary>
    /// 获取时长字符串
    /// </summary>
    /// <returns></returns>
    protected virtual string GetDurationStr() => Duration.Hours > 0 ? $"{Duration:hh\\:mm\\:ss}" : $"{Duration:mm\\:ss}";

    /// <summary>
    /// 获取流派字符串
    /// </summary>
    /// <returns></returns>
    protected virtual string GetGenreStr() => string.Join(", ", Genre);

    /// <summary>
    /// 获取发行年份字符串
    /// </summary>
    /// <returns></returns>
    protected string GetYearStr() => Year is 0 ? "" : Year.ToString();

    /// <summary>
    /// 获取文本前景色
    /// </summary>
    /// <param name="currentMusic"></param>
    /// <param name="isDarkTheme"></param>
    /// <returns>如果是当前播放歌曲, 返回主题色, 如果不是, 根据当前主题返回黑色或白色</returns>
    public SolidColorBrush GetTextForeground(DetailedMusicInfo currentMusic, bool isDarkTheme)
    {
        var isCurrentMusic = Path == currentMusic.Path;
        if (isCurrentMusic)
        {
            var color = isDarkTheme ? ColorHelper.FromArgb(0xFF, 0x42, 0x9C, 0xE3) : ColorHelper.FromArgb(0xFF, 0x00, 0x5A, 0x9E);
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(isDarkTheme ? Colors.White : Colors.Black);
    }
}

public class DetailedMusicInfo : BriefMusicInfo
{
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
    /// 时长字符串
    /// </summary>
    public override string DurationStr { get; set; } = "";

    /// <summary>
    /// 专辑艺术家数组
    /// </summary>
    private string[] AlbumArtists
    {
        get;
        set => field = [.. value
                    .SelectMany(artist => artist.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries))
                    .Distinct()];
    } = [];

    /// <summary>
    /// 专辑艺术家字符串, 为空时返回""
    /// </summary>
    public string AlbumArtistsStr { get; set; } = "";

    /// <summary>
    /// 艺术家和专辑名字符串
    /// </summary>
    public string ArtistAndAlbumStr { get; set; } = "";

    /// <summary>
    /// 清晰封面(可能为空)
    /// </summary>
    public override BitmapImage? Cover
    {
        get; set;
    }

    /// <summary>
    /// 封面缓冲数据
    /// </summary>
    public byte[] CoverBuffer { get; set; } = [];

    /// <summary>
    /// 比特率
    /// </summary>
    public string BitRate { get; set; } = "";

    /// <summary>
    /// 曲目
    /// </summary>
    public string Track { get; set; } = "";

    /// <summary>
    /// 歌词
    /// </summary>
    public string Lyric { get; set; } = "";

    public DetailedMusicInfo()
    {
    }

    public DetailedMusicInfo(string path)
    {
        Path = path;
        ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
        ItemType = System.IO.Path.GetExtension(path).ToLower();
        try
        {
            var musicFile = TagLib.File.Create(path);
            if (musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                CoverBuffer = coverBuffer;
                using var stream = new MemoryStream(coverBuffer);
                stream.Seek(0, SeekOrigin.Begin);
                Cover = new BitmapImage
                {
                    DecodePixelWidth = 400,
                    DecodePixelHeight = 400
                };
                Cover.SetSource(stream.AsRandomAccessStream());
            }
            Title = string.IsNullOrEmpty(musicFile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(path) : musicFile.Tag.Title;
            Album = musicFile.Tag.Album ?? "";
            Artists = [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers];
            ArtistsStr = GetArtistsStr();
            AlbumArtists = [.. musicFile.Tag.AlbumArtists];
            AlbumArtistsStr = GetAlbumArtistsStr();
            ArtistAndAlbumStr = GetArtistAndAlbumStr(ArtistsStr);
            Year = (ushort)musicFile.Tag.Year;
            YearStr = GetYearStr();
            Genre = musicFile.Tag.Genres;
            GenreStr = GetGenreStr();
            Duration = musicFile.Properties.Duration;
            DurationStr = GetDurationStr();
            Track = musicFile.Tag.Track == 0 ? "" : musicFile.Tag.Track.ToString();
            Lyric = musicFile.Tag.Lyrics ?? "";
            BitRate = $"{musicFile.Properties.AudioBitrate} kbps";
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException)
        {
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 获取参与创作的艺术家名字符串, 为空时返回""
    /// </summary>
    /// <returns></returns>
    protected override string GetArtistsStr() => string.Join(", ", Artists);

    /// <summary>
    /// 获取时长字符串
    /// </summary>
    /// <returns></returns>
    protected override string GetDurationStr() => Duration.Hours > 0 ? $"{Duration:hh\\:mm\\:ss}" : $"{Duration:mm\\:ss}";

    /// <summary>
    /// 获取流派字符串, 为空时返回""
    /// </summary>
    /// <returns></returns>
    protected override string GetGenreStr() => string.Join(", ", Genre);

    /// <summary>
    /// 获取专辑艺术家字符串, 为空时返回""
    /// </summary>
    /// <returns></returns>
    protected string GetAlbumArtistsStr() => string.Join(", ", AlbumArtists);

    /// <summary>
    /// 获取艺术家和专辑名字符串
    /// </summary>
    /// <returns></returns>
    protected string GetArtistAndAlbumStr(string artistsStr)
    {
        if (string.IsNullOrEmpty(artistsStr))
        {
            return Album ?? "";
        }
        if (string.IsNullOrEmpty(Album))
        {
            return artistsStr;
        }
        return $"{artistsStr} • {Album}";
    }
}