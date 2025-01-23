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

    public static string? SelectedAlbum
    {
        get; set;
    }
    public static string? SelectedArtist
    {
        get; set;
    }

    /// <summary>
    /// 软件显示名称
    /// </summary>
    public static readonly string AppDisplayName = "AppDisplayName".GetLocalized();

    /// <summary>
    /// 软件语言
    /// </summary>
    public static readonly string Language = "Data_Language".GetLocalized();

    /// <summary>
    /// 播放器支持的音频文件类型
    /// </summary>
    public static readonly string[] SupportedAudioTypes = [".flac", ".wav", ".m4a", ".aac", ".mp3", ".wma", ".ogg", ".oga", ".opus"];

    public static MusicPlayer MusicPlayer { get; set; } = new();
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static bool hasMusicLibraryLoaded { get; set; } = false;


    public static MainWindow? MainWindow
    {
        get; set;
    }
    public static MainViewModel? MainViewModel
    {
        get; set;
    }
    public static ShellPage? ShellPage
    {
        get; set;
    }
    public static MusicLibraryPage? MusicLibraryPage
    {
        get; set;
    }
    public static LyricPage? LyricPage
    {
        get; set;
    }
    public static RootPlayBarView? RootPlayBarView
    {
        get; set;
    }
    public static DesktopLyricWindow? DesktopLyricWindow
    {
        get; set;
    }
    public static SettingsViewModel? SettingsViewModel
    {
        get; set;
    }
    public static RootPlayBarViewModel? RootPlayBarViewModel
    {
        get; set;
    }
    public static HaveMusicViewModel? HaveMusicViewModel
    {
        get; set;
    }
    public static LocalSongsViewModel? LocalSongsViewModel
    {
        get; set;
    }
    public static LocalAlbumsViewModel? LocalAlbumsViewModel
    {
        get; set;
    }
}
