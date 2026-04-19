using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.LyricRenderer;

public sealed partial class LyricSlice(double startTime, string content) : ObservableObject
{
    public string Content { get; set; } = content;

    /// <summary>
    /// 歌词切片开始时间，单位为毫秒
    /// </summary>
    public double StartTime { get; set; } = startTime;

    /// <summary>
    /// 歌词切片结束时间，单位为毫秒
    /// </summary>
    public double EndTime { get; set; }

    /// <summary>
    /// 是否为当前播放的歌词
    /// </summary>
    public bool IsCurrent
    {
        get;
        set
        {
            field = value;
            UpdateStyle();
        }
    } = false;

    [ObservableProperty]
    public partial double FontSize { get; set; } = Settings.LyricPageNotCurrentFontSize;

    [ObservableProperty]
    public partial Thickness Margin { get; set; } = new(0, 20, 0, 20);

    [ObservableProperty]
    public partial double Opacity { get; set; } = 0.5;

    public void UpdateStyle()
    {
        FontSize = IsCurrent
            ? Settings.LyricPageCurrentFontSize
            : Settings.LyricPageNotCurrentFontSize;
        Margin = IsCurrent ? new Thickness(0, 40, 0, 40) : new Thickness(0, 20, 0, 20);
        Opacity = IsCurrent ? 1.0 : 0.5;
    }
}
