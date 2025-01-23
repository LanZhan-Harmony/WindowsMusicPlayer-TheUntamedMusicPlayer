using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;
using Windows.Storage.Streams;

namespace The_Untamed_Music_Player.Models;

public class BriefMusicInfo
{
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
    public string Album { get; set; } = "";

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
        set
        {
            if (value?.Length > 0)
            {
                var tempArtists = Array.Empty<string>();
                foreach (var i in value)
                {
                    if (i.Contains('、'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('、')];//将艺术家名字分割开并存到数组中
                    }
                    else if (i.Contains(','))
                    {
                        tempArtists = [.. tempArtists, .. i.Split(',')];
                    }
                    else if (i.Contains('，'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('，')];
                    }
                    else if (i.Contains('|'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('|')];
                    }
                    else if (i.Contains('/'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('/')];
                    }
                    else
                    {
                        tempArtists = [.. tempArtists, .. new[] { i }];
                    }
                }
                field = [.. tempArtists.Distinct()];
            }
            else
            {
                field = [];
            }
        }
    } = [];

    /// <summary>
    /// 参与创作的艺术家名, 为空时返回"未知艺术家"
    /// </summary>
    public string ArtistsStr { get; set; } = "";

    /// <summary>
    /// 获取参与创作的艺术家名
    /// </summary>
    /// <returns></returns>
    public string GetArtistsStr()
    {
        if (Artists.Length is 0)
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
    /// 时长
    /// </summary>
    public TimeSpan Duration
    {
        get; set;
    }

    /// <summary>
    /// 时长字符串
    /// </summary>
    public string DurationStr { get; set; } = "";

    /// <summary>
    /// 获取时长字符串
    /// </summary>
    /// <returns></returns>
    public string GetDurationStr()
    {
        return Duration.Hours > 0 ? $"{Duration:hh\\:mm\\:ss}" : $"{Duration:mm\\:ss}";
    }

    /// <summary>
    /// 发行年份
    /// </summary>
    public ushort Year
    {
        get; set;
    }

    /// <summary>
    /// 发行年份字符串, 为0时返回空字符串
    /// </summary>
    public string YearStr { get; set; } = "";

    /// <summary>
    /// 获取发行年份字符串
    /// </summary>
    /// <returns></returns>
    public string GetYearStr()
    {
        return Year is 0 ? "" : Year.ToString();
    }

    /// <summary>
    /// 封面(可能为空)
    /// </summary>
    public BitmapImage? Cover
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
    public string GenreStr { get; set; } = "";

    /// <summary>
    /// 获取流派字符串
    /// </summary>
    /// <returns></returns>
    public string GetGenre()
    {
        if (Genre.Length == 0)
        {
            return "MusicInfo_UnknownGenre".GetLocalized();
        }
        var sb = new StringBuilder();
        foreach (var genre in Genre)
        {
            sb.Append(genre);
            sb.Append(", ");
        }
        if (sb.Length > 0)
        {
            sb.Length -= 2;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate
    {
        get; set;
    }

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

    public BriefMusicInfo()
    {
    }

    public BriefMusicInfo(string path)
    {
        try
        {
            var musicFile = TagLib.File.Create(path);
            Path = path;
            ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            ItemType = System.IO.Path.GetExtension(path).ToLower();
            Album = musicFile.Tag.Album ?? "MusicInfo_UnknownAlbum".GetLocalized();
            Title = string.IsNullOrEmpty(musicFile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(path) : musicFile.Tag.Title;
            Artists = musicFile.Tag.AlbumArtists.Concat(musicFile.Tag.Performers).ToArray().Length != 0 ? [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers] : ["MusicInfo_UnknownArtist".GetLocalized()];
            ArtistsStr = GetArtistsStr();
            Year = (ushort)musicFile.Tag.Year;
            YearStr = GetYearStr();
            Genre = musicFile.Tag.Genres.Length != 0 ? [.. musicFile.Tag.Genres] : ["MusicInfo_UnknownGenre".GetLocalized()];
            GenreStr = GetGenre();
            Duration = musicFile.Properties.Duration;
            DurationStr = GetDurationStr();
        }
        catch
        {
            Path = path;
            ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            ItemType = System.IO.Path.GetExtension(path).ToLower();
            Album = "MusicInfo_UnknownAlbum".GetLocalized();
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
            Artists = ["MusicInfo_UnknownArtist".GetLocalized()];
            Genre = ["MusicInfo_UnknownGenre".GetLocalized()];
            GenreStr = GetGenre();
            Duration = TimeSpan.Zero;
            DurationStr = GetDurationStr();
        }
    }

    // 异步工厂方法
    public static async Task<BriefMusicInfo> CreateAsync(string path, string folder)
    {
        var info = new BriefMusicInfo();
        Task? coverTask = null;
        try
        {
            var musicFile = TagLib.File.Create(path);
            if (musicFile.Tag.Pictures != null && musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                coverTask = info.LoadCoverAsync(coverBuffer);
            }
            info.Path = path;
            info.Folder = folder;
            info.ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            info.ItemType = System.IO.Path.GetExtension(path).ToLower();
            info.Album = musicFile.Tag.Album ?? "MusicInfo_UnknownAlbum".GetLocalized();
            info.Title = string.IsNullOrEmpty(musicFile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(path) : musicFile.Tag.Title;
            info.Artists = [.. musicFile.Tag.AlbumArtists.Concat(musicFile.Tag.Performers).Distinct()];
            info.ArtistsStr = info.GetArtistsStr();
            info.Year = (ushort)musicFile.Tag.Year;
            info.YearStr = info.GetYearStr();
            info.Genre = musicFile.Tag.Genres.Length != 0 ? [.. musicFile.Tag.Genres] : ["MusicInfo_UnknownGenre".GetLocalized()];
            info.GenreStr = info.GetGenre();
            info.Duration = musicFile.Properties.Duration;
            info.DurationStr = info.GetDurationStr();

            // 等待 LoadCoverAsync 任务完成
            if (coverTask != null)
            {
                await coverTask;
            }
        }
        catch
        {
            // 设置默认值
            info.Path = path;
            info.Folder = folder;
            info.ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            info.ItemType = System.IO.Path.GetExtension(path).ToLower();
            info.Album = "MusicInfo_UnknownAlbum".GetLocalized();
            info.Title = System.IO.Path.GetFileNameWithoutExtension(path);
            info.Artists = ["MusicInfo_UnknownArtist".GetLocalized()];
            info.Genre = ["MusicInfo_UnknownGenre".GetLocalized()];
            info.GenreStr = info.GetGenre();
            info.Duration = TimeSpan.Zero;
            info.DurationStr = info.GetDurationStr();
        }
        return info;
    }

    // 异步加载封面方法
    private Task LoadCoverAsync(byte[] coverBuffer)
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
}

public class DetailedMusicInfo : BriefMusicInfo
{
    /// <summary>
    /// 专辑艺术家数组
    /// </summary>
    public string[] AlbumArtists
    {
        get;
        set
        {
            if (value?.Length > 0)
            {
                var tempArtists = Array.Empty<string>();
                foreach (var i in value)
                {
                    if (i.Contains('、'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('、')];
                    }
                    else if (i.Contains(','))
                    {
                        tempArtists = [.. tempArtists, .. i.Split(',')];
                    }
                    else if (i.Contains('，'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('，')];
                    }
                    else if (i.Contains('|'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('|')];
                    }
                    else if (i.Contains('/'))
                    {
                        tempArtists = [.. tempArtists, .. i.Split('/')];
                    }
                    else
                    {
                        tempArtists = [.. tempArtists, .. new[] { i }];
                    }
                }
                field = [.. tempArtists.Distinct()];
            }
            else
            {
                field = [];
            }
        }
    } = [];

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    public string GetAlbumArtistsStr()
    {
        if (AlbumArtists == null || AlbumArtists.Length == 0)
        {
            return "";
        }
        var sb = new StringBuilder();
        foreach (var artist in AlbumArtists)
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
    /// 艺术家和专辑名字符串
    /// </summary>
    public string ArtistAndAlbumStr { get; set; } = "";

    /// <summary>
    /// 获取艺术家和专辑名字符串
    /// </summary>
    /// <returns></returns>
    public string GetArtistAndAlbumStr()
    {
        var artistsStr = GetArtistsStr();
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

    /// <summary>
    /// 清晰封面(可能为空)
    /// </summary>
    public new BitmapImage? Cover
    {
        get; set;
    }

    /// <summary>
    /// 封面缓冲数据
    /// </summary>
    public byte[] CoverBuffer
    {
        get; set;
    } = [];

    /// <summary>
    /// 比特率
    /// </summary>
    public int BitRate
    {
        get; set;
    }

    /// <summary>
    /// 曲目
    /// </summary>
    public int Track
    {
        get; set;
    }

    /// <summary>
    /// 歌词
    /// </summary>
    public string Lyric { get; set; } = "";

    public DetailedMusicInfo()
    {
    }

    public DetailedMusicInfo(string path) : base(path)
    {
        try
        {
            var musicFile = TagLib.File.Create(path);
            AlbumArtists = [.. musicFile.Tag.AlbumArtists];
            ArtistAndAlbumStr = GetArtistAndAlbumStr();
            if (musicFile.Tag.Pictures != null && musicFile.Tag.Pictures.Length != 0)
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
            Track = (int)musicFile.Tag.Track;
            Lyric = musicFile.Tag.Lyrics ?? "";
            BitRate = musicFile.Properties.AudioBitrate;
        }
        catch
        {
            AlbumArtists = [];
            Track = 0;
            BitRate = 0;
        }
    }
}