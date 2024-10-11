using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class ArtistInfo
{
    private readonly string _name = "";
    /// <summary>
    /// 艺术家名
    /// </summary>
    public string Name => _name;

    private readonly HashSet<string> _albums = [];

    public readonly string? _genre;
    /// <summary>
    /// 艺术家流派
    /// </summary>
    public string? Genre => _genre;

    private readonly BitmapImage? _cover;
    /// <summary>
    /// 艺术家封面
    /// </summary>
    public BitmapImage? Cover => _cover;

    private TimeSpan _totalDuration;
    /// <summary>
    /// 艺术家歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => _totalDuration = value;
    }

    private int _totalMusicNum;
    /// <summary>
    /// 艺术家歌曲总数
    /// </summary>
    public int TotalMusicNum
    {
        get => _totalMusicNum;
        set => _totalMusicNum = value;
    }

    private int _totalAlbumNum;
    /// <summary>
    /// 艺术家专辑总数
    /// </summary>
    public int TotalAlbumNum
    {
        get => _totalAlbumNum;
        set => _totalAlbumNum = value;
    }

    public ArtistInfo(BriefMusicInfo briefMusicInfo, string name)
    {
        _name = name;
        TotalDuration = briefMusicInfo.Duration;
        _genre = briefMusicInfo.GenreStr;
        TotalMusicNum = 1;
        TotalAlbumNum = 1;
        _albums.Add(briefMusicInfo.Album);
    }

    /// <summary>
    /// 扫描歌曲时更新艺术家信息
    /// </summary>
    /// <param name="briefMusicInfo"></param>
    public void Update(BriefMusicInfo briefMusicInfo)
    {
        TotalDuration += briefMusicInfo.Duration;
        TotalMusicNum++;
        var album = briefMusicInfo.Album;

        if (_albums.Add(album))
        {
            TotalAlbumNum++;
        }
    }

    /// <summary>
    /// 清空专辑哈希列表
    /// </summary>
    public void ClearAlbums()
    {
        _albums.Clear();
    }

    /// <summary>
    /// 获取相册数量和歌曲数量
    /// </summary>
    /// <returns></returns>
    public string GetCount()
    {
        return $"{TotalAlbumNum} 个相册·{TotalMusicNum} 首歌曲·";
    }

    /// <summary>
    /// 获取总时长
    /// </summary>
    /// <returns></returns>
    public string GetDuration()
    {
        if (TotalDuration.Hours > 0)
        {
            return $"{TotalDuration.Hours} 小时 {TotalDuration.Minutes} 分钟 {TotalDuration.Seconds} 秒";
        }
        else
        {
            return $"{TotalDuration.Minutes} 分钟 {TotalDuration.Seconds} 秒";
        }
    }
}
