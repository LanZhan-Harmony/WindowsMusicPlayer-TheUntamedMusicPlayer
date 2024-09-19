using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class ArtistInfo
{
    //艺术家名
    private string _name = "";
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    private readonly HashSet<string> _albums = [];

    public string? _genre;
    public string? Genre
    {
        get => _genre;
        set => _genre = value;
    }

    //封面路径（当作头像，默认为检索到的第一张）
    private BitmapImage? _cover;
    public BitmapImage? Cover
    {
        get => _cover;
        set => _cover = value;
    }

    private TimeSpan _totalDuration;
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set => _totalDuration = value;
    }

    //歌曲数量
    private int _totalMusicNum;
    public int TotalMusicNum
    {
        get => _totalMusicNum;
        set => _totalMusicNum = value;
    }

    private int _totalAlbumNum;
    public int TotalAlbumNum
    {
        get => _totalAlbumNum;
        set => _totalAlbumNum = value;
    }

    public ArtistInfo(BriefMusicInfo briefMusicInfo, string name)
    {
        Name = name;
        Cover = briefMusicInfo.Cover;
        TotalDuration = briefMusicInfo.Duration;
        Genre = briefMusicInfo.GenreStr;
        TotalMusicNum = 1;
        TotalAlbumNum = 1;
        _albums.Add(briefMusicInfo.Album);
    }

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

    public void ClearAlbums()
    {
        _albums.Clear();
    }

    public string? GetCount()
    {
        return $"{TotalAlbumNum} 个相册·{TotalMusicNum} 首歌曲·";
    }

    public string? GetDuration()
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
