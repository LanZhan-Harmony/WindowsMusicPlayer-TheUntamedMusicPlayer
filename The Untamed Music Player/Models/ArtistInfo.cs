using System.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class ArtistInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //艺术家名
    private string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private Dictionary<string, AlbumInfo>? _albums = [];
    public Dictionary<string, AlbumInfo>? Albums
    {
        get => _albums;
        set
        {
            _albums = value;
            OnPropertyChanged(nameof(Albums));
        }
    }

    public string? _genre;
    public string? Genre
    {
        get => _genre;
        set
        {
            _genre = value;
            OnPropertyChanged(nameof(Genre));
        }
    }

    //封面路径（当作头像，默认为检索到的第一张）
    private BitmapImage? _cover;
    public BitmapImage? Cover
    {
        get => _cover;
        set
        {
            _cover = value;
            OnPropertyChanged(nameof(Cover));
        }
    }

    private TimeSpan _totalDuration;
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set
        {
            _totalDuration = value;
            OnPropertyChanged(nameof(TotalDuration));
        }
    }

    //歌曲数量
    private int _totalMusicNum;
    public int TotalMusicNum
    {
        get => _totalMusicNum;
        set
        {
            _totalMusicNum = value;
            OnPropertyChanged(nameof(TotalMusicNum));
        }
    }

    private int _totalAlbumNum;
    public int TotalAlbumNum
    {
        get => _totalAlbumNum;
        set
        {
            _totalAlbumNum = value;
            OnPropertyChanged(nameof(TotalAlbumNum));
        }
    }

    public ArtistInfo(BriefMusicInfo briefMusicInfo, string name)
    {
        Name = name;
        Cover = briefMusicInfo.Cover;
        TotalDuration = briefMusicInfo.Duration;
        Genre = briefMusicInfo.GenreStr;
        TotalMusicNum = 1;
        TotalAlbumNum = 1;
        if (briefMusicInfo.Album != null && Albums != null)
        {
            Albums[briefMusicInfo.Album] = new AlbumInfo(briefMusicInfo);
        }
    }

    public void Update(BriefMusicInfo briefMusicInfo)
    {
        TotalDuration += briefMusicInfo.Duration;
        TotalMusicNum++;
        var album = briefMusicInfo.Album;
        if (album != null && Albums != null)
        {
            if (!Albums.TryGetValue(album, out var value))
            {
                Albums[album] = new AlbumInfo(briefMusicInfo);
                TotalAlbumNum++;
            }
            else
            {
                value.Update(briefMusicInfo);
            }
        }
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
