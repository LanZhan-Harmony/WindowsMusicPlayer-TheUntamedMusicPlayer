using Microsoft.UI.Xaml.Media.Imaging;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IArtistInfoBase
{
    string Name { get; set; }
    BitmapImage? Cover { get; set; }
    string? CoverPath { get; set; }
}
