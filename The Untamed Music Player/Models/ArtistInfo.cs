using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class ArtistInfo
{
    private readonly HashSet<string> _albums = [];

    /// <summary>
    /// 艺术家名
    /// </summary>
    public string Name
    {
        get; set;
    } = "";


    /// <summary>
    /// 艺术家流派
    /// </summary>
    public string Genre
    {
        get; set;
    } = "";

    /// <summary>
    /// 艺术家封面
    /// </summary>
    public BitmapImage? Cover
    {
        get; set;
    }

    /// <summary>
    /// 艺术家歌曲总时长
    /// </summary>
    public TimeSpan TotalDuration
    {
        get; set;
    }

    /// <summary>
    /// 艺术家歌曲总数
    /// </summary>
    public int TotalMusicNum
    {
        get; set;
    }

    /// <summary>
    /// 艺术家专辑总数
    /// </summary>
    public int TotalAlbumNum
    {
        get; set;
    }

    public ArtistInfo(BriefMusicInfo briefMusicInfo, string name)
    {
        Name = name;
        TotalDuration = briefMusicInfo.Duration;
        Genre = briefMusicInfo.GenreStr;
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
