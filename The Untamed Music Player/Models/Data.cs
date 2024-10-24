using System.Diagnostics;
using The_Untamed_Music_Player.Helpers;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;
using Windows.UI;

namespace The_Untamed_Music_Player.Models;

public static class Data
{
    public static bool NotFirstUsed { get; set; } = false;
    public static byte SelectedMaterial { get; set; } = 3;
    public static bool IsFallBack { get; set; } = true;
    public static byte LuminosityOpacity { get; set; } = 60;
    public static Color TintColor
    {
        get;
        set;
    }

    public static readonly string AppDisplayName = "AppDisplayName".GetLocalized();
    public static readonly string Language = "Data_Language".GetLocalized();
    public static readonly string[] SupportedAudioTypes = [".flac", ".wav", ".m4a", ".aac", ".mp3", ".wma", ".ogg", ".oga", ".opus"];

    public static MusicPlayer MusicPlayer { get; set; } = new();
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static string? SelectedAlbum;
    public static string? SelectedArtist;
    public static MainWindow? MainWindow;
    public static MainViewModel? MainViewModel;
    public static ShellPage? ShellPage;
    public static SettingsViewModel? SettingsViewModel;
    public static RootPlayBarViewModel? RootPlayBarViewModel;
    public static LocalSongsViewModel? LocalSongsViewModel;
    public static LocalAlbumsViewModel? LocalAlbumsViewModel;

}
