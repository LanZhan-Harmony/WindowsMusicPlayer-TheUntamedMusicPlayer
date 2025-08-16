using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;

namespace The_Untamed_Music_Player.Models;

public class PlaylistInfo
{
    public string Name { get; set; }
    public string TotalSongNumStr { get; set; }
    public long ModifiedDate { get; set; }
    public BitmapImage? Cover { get; set; }
    public List<string>? CoverPath { get; set; }
    public List<IBriefSongInfoBase> SongList { get; set; }

    public PlaylistInfo() { }
}
