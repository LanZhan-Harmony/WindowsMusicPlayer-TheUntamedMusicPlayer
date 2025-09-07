using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.Models;

public static class Data
{
    /// <summary>
    /// 是否是第一次使用本软件
    /// </summary>
    public static bool? NotFirstUsed { get; set; } = null;

    /// <summary>
    /// 是否正在下载或更改音乐
    /// </summary>
    public static bool IsMusicProcessing { get; set; } = false;

    /// <summary>
    /// 是否为文件激活启动（通过文件关联启动）
    /// </summary>
    public static bool IsFileActivationLaunch { get; set; } = false;

    public static LocalAlbumInfo? SelectedLocalAlbum { get; set; }
    public static LocalArtistInfo? SelectedLocalArtist { get; set; }
    public static PlaylistInfo? SelectedPlaylist { get; set; }
    public static IBriefOnlineAlbumInfo? SelectedOnlineAlbum { get; set; }
    public static IBriefOnlineArtistInfo? SelectedOnlineArtist { get; set; }
    public static IBriefOnlinePlaylistInfo? SelectedOnlinePlaylist { get; set; }

    /// <summary>
    /// 软件显示名称
    /// </summary>
    public static readonly string AppDisplayName = "AppDisplayName".GetLocalized();

    /// <summary>
    /// 播放器支持的音频文件类型
    /// </summary>
    public static readonly string[] SupportedAudioTypes =
    [
        ".mp3",
        ".flac",
        ".ogg",
        ".m4a",
        ".wav",
        ".mp2",
        ".mp1",
        ".aif",
        ".aiff",
        ".m2a",
        ".m1a",
        ".mp3pro",
        ".bwf",
    ];

    public static readonly string[] SupportedCoverTypes =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".jpe",
        ".jfif",
        ".bmp",
        ".dip",
        ".gif",
        ".tif",
        ".tiff",
    ];

    /// <summary>
    /// 是否为独占模式
    /// </summary>
    public static bool IsExclusiveMode { get; set; }

    /// <summary>
    /// 是否为如果当前位于音乐库歌曲页面且使用文件夹排序方式，点击歌曲仅会将其所在文件夹内的歌曲加入播放队列
    /// </summary>
    public static bool IsOnlyAddSpecificFolder { get; set; }

    /// <summary>
    /// 歌词字体
    /// </summary>
    public static FontFamily SelectedFontFamily { get; set; } = new("Microsoft YaHei");

    /// <summary>
    /// 歌词字号
    /// </summary>
    public static double SelectedCurrentFontSize { get; set; } = 50.0;
    public static double SelectedNotCurrentFontSize { get; set; } = 20.0;

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    public static bool IsWindowBackgroundFollowsCover { get; set; } = false;

    public static OnlineMusicLibrary OnlineMusicLibrary { get; set; } = new();
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static PlaylistLibrary PlaylistLibrary { get; set; } = new();
    public static MusicPlayer MusicPlayer { get; set; } = new();

    public static MainWindow? MainWindow { get; set; }
    public static ShellPage? ShellPage { get; set; }
    public static HomePage HomePage { get; set; } = null!;
    public static LyricPage? LyricPage { get; set; }
    public static RootPlayBarView? RootPlayBarView { get; set; }
    public static DesktopLyricWindow? DesktopLyricWindow { get; set; }
    public static MainViewModel? MainViewModel { get; set; }
    public static SettingsViewModel? SettingsViewModel { get; set; }
    public static ShellViewModel? ShellViewModel { get; set; }
    public static RootPlayBarViewModel? RootPlayBarViewModel { get; set; }
    public static LocalSongsViewModel? LocalSongsViewModel { get; set; }
    public static LocalAlbumsViewModel? LocalAlbumsViewModel { get; set; }
}
