using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace UntamedMediaPlayer.Core.Models.Settings;

public sealed partial class LibrarySettings : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<string> MusicFolders { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> VideoFolders { get; set; } = [];

    [ObservableProperty]
    public partial string MusicDownloadFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
}
