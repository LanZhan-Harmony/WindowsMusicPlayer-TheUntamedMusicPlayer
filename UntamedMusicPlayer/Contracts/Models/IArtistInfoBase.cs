using Microsoft.UI.Xaml.Media.Imaging;

namespace UntamedMusicPlayer.Contracts.Models;

public interface IArtistInfoBase
{
    string Name { get; set; }
    BitmapImage? Cover { get; set; }
    string? CoverPath { get; set; }
}
