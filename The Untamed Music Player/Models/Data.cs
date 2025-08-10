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
    public static bool NotFirstUsed { get; set; } = false;

    /// <summary>
    /// 是否已经加载了音乐库
    /// </summary>
    public static bool HasMusicLibraryLoaded { get; set; } = false;

    /// <summary>
    /// 是否正在下载或更改音乐
    /// </summary>
    public static bool IsMusicProcessing { get; set; } = false;

    public static LocalAlbumInfo? SelectedLocalAlbum { get; set; }
    public static LocalArtistInfo? SelectedLocalArtist { get; set; }
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
        ".midi",
        ".mp2",
        ".mp1",
        ".aif",
        ".aiff",
        ".m2a",
        ".m1a",
        ".mp3pro",
        ".bwf",
        ".mus",
        ".mod",
        ".mid",
        ".mo3",
        ".s3m",
        ".xm",
        ".it",
        ".mtm",
        ".umx",
    ];

    /// <summary>
    /// 歌词字体
    /// </summary>
    public static FontFamily SelectedFontFamily { get; set; } = new("Microsoft YaHei");

    /// <summary>
    /// 歌词字号
    /// </summary>
    public static double SelectedFontSize { get; set; } = 50.0;

    /// <summary>
    /// 是否显示歌词背景
    /// </summary>
    public static bool IsWindowBackgroundFollowsCover { get; set; } = false;

    public static OnlineMusicLibrary OnlineMusicLibrary { get; set; } = new();
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static MusicPlayer MusicPlayer { get; set; } = new();

    public static MainWindow? MainWindow { get; set; }
    public static ShellPage? ShellPage { get; set; }
    public static HomePage HomePage { get; set; } = null!;
    public static MusicLibraryPage? MusicLibraryPage { get; set; }
    public static LyricPage? LyricPage { get; set; }
    public static RootPlayBarView? RootPlayBarView { get; set; }
    public static DesktopLyricWindow? DesktopLyricWindow { get; set; }
    public static MainViewModel? MainViewModel { get; set; }
    public static SettingsViewModel? SettingsViewModel { get; set; }
    public static ShellViewModel? ShellViewModel { get; set; }
    public static RootPlayBarViewModel? RootPlayBarViewModel { get; set; }
    public static HaveMusicViewModel? HaveMusicViewModel { get; set; }
    public static LocalSongsViewModel? LocalSongsViewModel { get; set; }
    public static LocalAlbumsViewModel? LocalAlbumsViewModel { get; set; }
}
