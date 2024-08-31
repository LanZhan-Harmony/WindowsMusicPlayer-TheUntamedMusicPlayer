using System.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Models;
public class AlbumInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //专辑名
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

    //专辑封面路径
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

    private string? _artist;
    public string? Artist
    {
        get => _artist;
        set
        {
            _artist = value;
            OnPropertyChanged(nameof(Artist));
        }
    }

    //专辑包含的歌曲数量
    private int _totalNum;
    public int TotalNum
    {
        get => _totalNum;
        set
        {
            _totalNum = value;
            OnPropertyChanged(nameof(TotalNum));
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

    //专辑发布年份
    private string _year = "";
    public string Year
    {
        get => _year;
        set
        {
            _year = value;
            OnPropertyChanged(nameof(Year));
        }
    }

    private DateTimeOffset _modifiedDate;
    /// <summary>
    /// 修改日期
    /// </summary>
    public DateTimeOffset ModifiedDate
    {
        get => _modifiedDate;
        set => _modifiedDate = value;
    }

    public AlbumInfo()
    {
    }
    public AlbumInfo(BriefMusicInfo briefmusicInfo)
    {
        Name = briefmusicInfo.Album;
        Year = briefmusicInfo.YearStr;
        ModifiedDate = briefmusicInfo.ModifiedDate;
        Cover = briefmusicInfo.Cover;
        Artist = briefmusicInfo.ArtistsStr;
        TotalDuration = briefmusicInfo.Duration;
        TotalNum = 1;
    }

    public void Update(BriefMusicInfo briefmusicInfo)
    {
        TotalNum++;
        TotalDuration += briefmusicInfo.Duration;
    }

    public string? GetCountAndDuration()
    {
        if (TotalDuration.Hours > 0)
        {
            return string.IsNullOrEmpty(Year)
                ? $"{TotalNum} 首歌曲·{TotalDuration:hh\\:mm\\:ss} 歌曲长度"
                : $"{Year}·{TotalNum} 首歌曲·{TotalDuration:hh\\:mm\\:ss} 歌曲长度";
        }
        else
        {
            return string.IsNullOrEmpty(Year)
                ? $"{TotalNum} 首歌曲·{TotalDuration:mm\\:ss} 歌曲长度"
                : $"{Year}·{TotalNum} 首歌曲·{TotalDuration:mm\\:ss} 歌曲长度";
        }
    }
}
