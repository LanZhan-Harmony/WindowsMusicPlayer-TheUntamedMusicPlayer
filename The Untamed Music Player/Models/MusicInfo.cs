using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Helpers;

namespace The_Untamed_Music_Player.Models;

public class BriefMusicInfo
{
    private readonly string _path = "";
    /// <summary>
    /// 文件位置
    /// </summary>
    public string Path => _path;

    private readonly string _folder = "";
    /// <summary>
    /// 所处文件夹
    /// </summary>
    public string Folder => _folder;

    private readonly string _itemType = "";
    /// <summary>
    /// 项目类型
    /// </summary>
    public string ItemType => _itemType;

    private readonly string _album = "";
    /// <summary>
    /// 专辑名, 为空时返回"未知专辑"
    /// </summary>
    public string Album => _album;

    private readonly string _title = "";
    /// <summary>
    /// 歌曲名
    /// </summary>
    public string Title => _title;

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
                _artists = [.. tempArtists.Distinct()];
            }
            else
            {
                _artists = [];
            }
        }
    }

    private readonly string _artistsStr = "";
    /// <summary>
    /// 参与创作的艺术家名, 为空时返回"未知艺术家"
    /// </summary>
    public string ArtistsStr => _artistsStr;

    /// <summary>
    /// 获取参与创作的艺术家名
    /// </summary>
    /// <returns></returns>
    public string GetArtistsStr()
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

    private readonly TimeSpan _duration;
    /// <summary>
    /// 时长
    /// </summary>
    public TimeSpan Duration => _duration;

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

    private readonly ushort _year;
    /// <summary>
    /// 发行年份
    /// </summary>
    public ushort Year => _year;

    private readonly string _yearStr = "";
    /// <summary>
    /// 发行年份字符串, 为0时返回空字符串
    /// </summary>
    public string YearStr => _yearStr;

    /// <summary>
    /// 获取发行年份字符串
    /// </summary>
    /// <returns></returns>
    public string GetYearStr()
    {
        return Year == 0 ? "" : Year.ToString();
    }

    private readonly BitmapImage? _cover;
    /// <summary>
    /// 封面(可能为空)
    /// </summary>
    public BitmapImage? Cover => _cover;

    private readonly string[] _genre = [];
    /// <summary>
    /// 流派数组
    /// </summary>
    public string[] Genre => _genre;

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

    private readonly long _modifiedDate;
    /// <summary>
    /// 修改日期
    /// </summary>
    public long ModifiedDate => _modifiedDate;

    /// <summary>
    /// 获取文本前景色
    /// </summary>
    /// <param name="currentMusic"></param>
    /// <param name="elementTheme"></param>
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
            _path = path;
            _modifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            _itemType = System.IO.Path.GetExtension(path).ToLower();
            _album = musicFile.Tag.Album ?? "MusicInfo_UnknownAlbum".GetLocalized();
            _title = string.IsNullOrEmpty(musicFile.Tag.Title) ? System.IO.Path.GetFileNameWithoutExtension(path) : musicFile.Tag.Title;
            Artists = musicFile.Tag.AlbumArtists.Concat(musicFile.Tag.Performers).ToArray().Length != 0 ? [.. musicFile.Tag.AlbumArtists, .. musicFile.Tag.Performers] : ["MusicInfo_UnknownArtist".GetLocalized()];
            _artistsStr = GetArtistsStr();
            _year = (ushort)musicFile.Tag.Year;
            _yearStr = GetYearStr();
            _genre = musicFile.Tag.Genres.Length != 0 ? [.. musicFile.Tag.Genres] : ["MusicInfo_UnknownGenre".GetLocalized()];
            _genreStr = GetGenre();
            if (musicFile.Tag.Pictures != null && musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                using var stream = new MemoryStream(coverBuffer);
                stream.Seek(0, SeekOrigin.Begin);
                _cover = new BitmapImage
                {
                    DecodePixelWidth = 160,
                    DecodePixelHeight = 160
                };
                _cover.SetSource(stream.AsRandomAccessStream());
            }
            _duration = musicFile.Properties.Duration;
            _durationStr = GetDurationStr();
        }
        catch (Exception)
        {
            _path = path;
            _modifiedDate = new DateTimeOffset(new FileInfo(path).LastWriteTime).ToUnixTimeSeconds();
            _itemType = System.IO.Path.GetExtension(path).ToLower();
            _album = "MusicInfo_UnknownAlbum".GetLocalized();
            _title = System.IO.Path.GetFileNameWithoutExtension(path);
            Artists = ["MusicInfo_UnknownArtist".GetLocalized()];
            _genre = ["MusicInfo_UnknownGenre".GetLocalized()];
            _genreStr = GetGenre();
            _duration = TimeSpan.Zero;
            _durationStr = GetDurationStr();
        }
    }

    public BriefMusicInfo(string path, string folder) : this(path)
    {
        _folder = folder;
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
                _albumArtists = [.. tempArtists.Distinct()];
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
    public string GetAlbumArtistsStr()
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

    public readonly string _artistAndAlbumStr = "";
    /// <summary>
    /// 艺术家和专辑名字符串
    /// </summary>
    public string ArtistAndAlbumStr => _artistAndAlbumStr;

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

    private readonly BitmapImage? _cover;
    /// <summary>
    /// 清晰封面(可能为空)
    /// </summary>
    public new BitmapImage? Cover => _cover;

    private readonly int _bitRate;
    /// <summary>
    /// 比特率
    /// </summary>
    public int BitRate => _bitRate;

    private readonly int _track;
    /// <summary>
    /// 曲目
    /// </summary>
    public int Track => _track;

    private readonly string _lyric = "";
    /// <summary>
    /// 歌词
    /// </summary>
    public string Lyric => _lyric;

    public DetailedMusicInfo()
    {
    }

    public DetailedMusicInfo(string path) : base(path)
    {
        try
        {
            var musicFile = TagLib.File.Create(path);
            AlbumArtists = [.. musicFile.Tag.AlbumArtists];
            _artistAndAlbumStr = GetArtistAndAlbumStr();
            if (musicFile.Tag.Pictures != null && musicFile.Tag.Pictures.Length != 0)
            {
                var coverBuffer = musicFile.Tag.Pictures[0].Data.Data;
                using var stream = new MemoryStream(coverBuffer);
                stream.Seek(0, SeekOrigin.Begin);
                _cover = new BitmapImage
                {
                    DecodePixelWidth = 400,
                    DecodePixelHeight = 400
                };
                _cover.SetSource(stream.AsRandomAccessStream());
            }
            _track = (int)musicFile.Tag.Track;
            _lyric = musicFile.Tag.Lyrics ?? "";
            _bitRate = musicFile.Properties.AudioBitrate;
        }
        catch (Exception)
        {
            AlbumArtists = [];
            _track = 0;
            _bitRate = 0;
        }
    }
}