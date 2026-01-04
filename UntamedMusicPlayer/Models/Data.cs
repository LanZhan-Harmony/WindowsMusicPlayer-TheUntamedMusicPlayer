using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Helpers;
using UntamedMusicPlayer.LyricRenderer;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Models;

public static class Data
{
    /// <summary>
    /// 是否正在下载或更改音乐
    /// </summary>
    public static bool IsMusicProcessing { get; set; } = false;

    /// <summary>
    /// 是否为文件激活启动（通过文件关联启动）
    /// </summary>
    public static bool IsFileActivationLaunch { get; set; } = false;

    /// <summary>
    /// 软件显示名称
    /// </summary>
    public static readonly string AppDisplayName = "AppDisplayName".GetLocalized();

    /// <summary>
    /// 支持的音频文件类型
    /// </summary>
    public static readonly string[] SupportedAudioTypes =
    [
        ".mp3",
        ".flac",
        ".ogg",
        ".m4a",
        ".wav",
        ".opus",
        ".dsf",
        ".dff",
        ".mid",
        ".midi",
        ".cda",
        ".ape",
        ".webm",
        ".wv",
        ".mp2",
        ".mp1",
        ".aif",
        ".aiff",
        ".m2a",
        ".m1a",
        ".mp3pro",
        ".bwf",
    ];

    /// <summary>
    /// 支持的封面图片类型
    /// </summary>
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

    #region 用于导航的信息
    public static LocalAlbumInfo? SelectedLocalAlbum { get; set; }
    public static LocalArtistInfo? SelectedLocalArtist { get; set; }
    public static PlaylistInfo? SelectedPlaylist { get; set; }
    public static IBriefOnlineAlbumInfo? SelectedOnlineAlbum { get; set; }
    public static IBriefOnlineArtistInfo? SelectedOnlineArtist { get; set; }
    public static IBriefOnlinePlaylistInfo? SelectedOnlinePlaylist { get; set; }
    #endregion

    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static OnlineMusicLibrary OnlineMusicLibrary { get; set; } = new();
    public static PlaylistLibrary PlaylistLibrary { get; set; } = new();
    public static PlayQueueManager PlayQueueManager { get; set; } = null!;
    public static LyricManager LyricManager { get; set; } = null!;
    public static SharedPlaybackState PlayState { get; set; } = null!;
    public static MusicPlayer MusicPlayer { get; set; } = new();

    #region Views
    public static MainWindow? MainWindow { get; set; }
    public static ShellPage? ShellPage { get; set; }
    public static HomePage? HomePage { get; set; }
    public static LyricPage? LyricPage { get; set; }
    public static RootPlayBarView? RootPlayBarView { get; set; }
    public static DesktopLyricWindow? DesktopLyricWindow { get; set; }
    public static OnlineSongsPage? OnlineSongsPage { get; set; }
    #endregion

    #region ViewModels
    public static SettingsViewModel? SettingsViewModel { get; set; }
    public static ShellViewModel? ShellViewModel { get; set; }
    public static RootPlayBarViewModel? RootPlayBarViewModel { get; set; }
    public static LocalSongsViewModel? LocalSongsViewModel { get; set; }
    public static LocalAlbumsViewModel? LocalAlbumsViewModel { get; set; }
    #endregion
}
