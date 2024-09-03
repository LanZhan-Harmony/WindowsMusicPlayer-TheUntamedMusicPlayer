using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;

public class BriefMusicInfo
{
    private string _path = "";
    /// <summary>
    /// 文件位置
    /// </summary>
    public string Path
    {
        get => _path;
        set => _path = value;
    }

    private string _itemType = "";
    /// <summary>
    /// 项目类型
    /// </summary>
    public string ItemType
    {
        get => _itemType;
        set => _itemType = value;
    }

    private string _album = "";
    /// <summary>
    /// 专辑名, 为空时返回"未知专辑"
    /// </summary>
    public string Album
    {
        get => _album;
        set => _album = value;
    }

    private string _title = "";
    /// <summary>
    /// 歌曲名
    /// </summary>
    public string Title
    {
        get => _title;
        set => _title = value;
    }

    private string[] _artists = [];
    /// <summary>
    /// 参与创作的艺术家数组
    /// </summary>
    public string[] Artists
    {
        get => _artists;
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
                _artists = tempArtists.Distinct().ToArray();
            }
            else
            {
                _artists = [];
            }
            ArtistsStr = GetArtists();
        }
    }

    private string _artistsStr = "";
    /// <summary>
    /// 参与创作的艺术家名, 为空时返回"未知艺术家"
    /// </summary>
    public string ArtistsStr
    {
        get => _artistsStr;
        set => _artistsStr = value;
    }

    /// <summary>
    /// 获取参与创作的艺术家名
    /// </summary>
    /// <returns></returns>
    public string GetArtists()
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

    private TimeSpan _duration;
    /// <summary>
    /// 时长
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            DurationStr = GetDurationStr();
        }
    }

    private string _durationStr = "";
    /// <summary>
    /// 时长字符串
    /// </summary>
    public string DurationStr
    {
        get => _durationStr;
        set => _durationStr = value;
    }

    /// <summary>
    /// 获取时长字符串
    /// </summary>
    /// <returns></returns>
    public string GetDurationStr()
    {
        if (Duration.Hours > 0)
        {
            return $"{Duration:hh\\:mm\\:ss}";
        }
        else
        {
            return $"{Duration:mm\\:ss}";
        }
    }

    private ushort _year;
    /// <summary>
    /// 发行年份
    /// </summary>
    public ushort Year
    {
        get => _year;
        set
        {
            _year = value;
            YearStr = value.ToString();
        }
    }

    private string _yearStr = "";
    /// <summary>
    /// 发行年份字符串, 为0时返回空字符串
    /// </summary>
    public string YearStr
    {
        get => _yearStr;
        set
        {
            if (value == "0")
            {
                _yearStr = "";
            }
            else
            {
                _yearStr = value;
            }
        }
    }

    private BitmapImage? _cover;
    /// <summary>
    /// 封面(可能为空)
    /// </summary>
    public BitmapImage? Cover
    {
        get => _cover;
        set => _cover = value;
    }

    private string[] _genre = [];
    /// <summary>
    /// 流派数组
    /// </summary>
    public string[] Genre
    {
        get => _genre;
        set
        {
            _genre = value;
            GenreStr = GetGenre();
        }
    }

    private string _genreStr = "";
    /// <summary>
    /// 流派字符串, 为空时返回"未知流派"
    /// </summary>
    public string GenreStr
    {
        get => _genreStr;
        set => _genreStr = value;
    }

    /// <summary>
    /// 获取流派字符串
    /// </summary>
    /// <returns></returns>
    public string GetGenre()
    {
        if (_genre == null || _genre.Length == 0)
        {
            return "MusicInfo_UnknownGenre".GetLocalized();
        }
        var sb = new StringBuilder();
        foreach (var genre in _genre)
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

    private long _modifiedDate;
    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate
    {
        get => _modifiedDate;
        set => _modifiedDate = value;
    }

    /// <summary>
    /// 获取文本前景色
    /// </summary>
    /// <param name="currentMusic"></param>
    /// <param name="elementTheme"></param>
    /// <returns>如果是当前播放歌曲, 返回主题色, 如果不是, 根据当前主题返回黑色或白色</returns>
    public SolidColorBrush GetTextForeground(DetailedMusicInfo currentMusic, ElementTheme elementTheme)
    {
        if (Path == currentMusic.Path)
        {
            return (SolidColorBrush)App.Current.Resources["AccentTextFillColorTertiaryBrush"];
        }

        if (elementTheme == ElementTheme.Dark)
        {
            return new SolidColorBrush(Microsoft.UI.Colors.White);
        }
        else
        {
            return new SolidColorBrush(Microsoft.UI.Colors.Black);
        }
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
            Year = (ushort)musicFile.Tag.Year;
            Genre = musicFile.Tag.Genres.Length != 0 ? [.. musicFile.Tag.Genres] : ["MusicInfo_UnknownGenre".GetLocalized()];
            if (musicFile.Tag.Pictures != null && musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                using var stream = new MemoryStream(coverBuffer);
                stream.Seek(0, SeekOrigin.Begin);
                Cover = new BitmapImage
                {
                    DecodePixelWidth = 600,
                    DecodePixelHeight = 600
                };
                Cover.SetSource(stream.AsRandomAccessStream());
            }
            Duration = musicFile.Properties.Duration;
        }
        catch (Exception)
        {
            Path = path;
            ModifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            ItemType = System.IO.Path.GetExtension(path).ToLower();
            Album = "MusicInfo_UnknownAlbum".GetLocalized();
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
            Artists = ["MusicInfo_UnknownArtist".GetLocalized()];
            Genre = ["MusicInfo_UnknownGenre".GetLocalized()];
            Duration = TimeSpan.Zero;
        }
    }
}

public class DetailedMusicInfo : BriefMusicInfo
{
    private string[] _albumArtists = [];
    /// <summary>
    /// 专辑艺术家数组
    /// </summary>
    public string[] AlbumArtists
    {
        get => _albumArtists;
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
                _albumArtists = tempArtists.Distinct().ToArray();
            }
            else
            {
                _albumArtists = [];
            }
        }
    }

    /// <summary>
    /// 获取专辑艺术家字符串
    /// </summary>
    /// <returns></returns>
    public string GetAlbumArtists()
    {
        if (_albumArtists == null || _albumArtists.Length == 0)
        {
            return "";
        }
        var sb = new StringBuilder();
        foreach (var artist in _albumArtists)
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

    public string _artistAndAlbumStr = "";
    /// <summary>
    /// 艺术家和专辑名字符串
    /// </summary>
    public string ArtistAndAlbumStr
    {
        get => _artistAndAlbumStr;
        set => _artistAndAlbumStr = value;
    }

    /// <summary>
    /// 获取艺术家和专辑名字符串
    /// </summary>
    /// <returns></returns>
    public string GetArtistAndAlbumStr()
    {
        var artistsStr = GetArtists();
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

    private int _bitRate;
    /// <summary>
    /// 比特率
    /// </summary>
    public int BitRate
    {
        get => _bitRate;
        set => _bitRate = value;
    }

    private int _track;
    /// <summary>
    /// 曲目
    /// </summary>
    public int Track
    {
        get => _track;
        set => _track = value;
    }

    private string _lyric = "";
    /// <summary>
    /// 歌词
    /// </summary>
    public string Lyric
    {
        get => _lyric;
        set => _lyric = value;
    }

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
            Track = (int)musicFile.Tag.Track;
            Lyric = musicFile.Tag.Lyrics ?? "";
            BitRate = musicFile.Properties.AudioBitrate;
        }
        catch (Exception)
        {
            AlbumArtists = [];
            Track = 0;
            BitRate = 0;
        }
    }
}