using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IArtistInfoBase
{
    string Name { get; set; }
    BitmapImage? Cover { get; set; }
    string? CoverPath { get; set; }
    TimeSpan TotalDuration { get; set; }
    int TotalSongNum { get; set; }
    int TotalAlbumNum { get; set; }
    byte[] GetCoverBytes();
    string GetCountStr();
    string GetDurationStr();
    string GetDescriptionStr();
}
