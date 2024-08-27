using The_Untamed_Music_Player.Contracts.Services;
using The_Untamed_Music_Player.ViewModels;
using The_Untamed_Music_Player.Views;

namespace The_Untamed_Music_Player.Models;

public static class Data
{
    public static readonly string[] SupportedAudioTypes = [".flac", ".wav", ".m4a", ".aac", ".mp3", ".wma", ".ogg", ".oga", ".opus"];

    public static MusicPlayer MusicPlayer { get; set; } = new(App.GetService<ILocalSettingsService>());
    public static MusicLibrary MusicLibrary { get; set; } = new();
    public static string? SelectedAlbum;
    public static string? SelectedArtist;
    public static bool IsDetail = false;
    public static MainWindow? MainWindow;
    public static ShellPage? ShellPage;
    public static SettingsViewModel? SettingsViewModel;
    public static RootPlayBarViewModel? RootPlayBarViewModel;
    public static 歌曲ViewModel? 歌曲ViewModel;
}
