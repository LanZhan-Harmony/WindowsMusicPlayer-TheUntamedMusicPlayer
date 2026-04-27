using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace UntamedMusicPlayer.Models;

public sealed class FontFamilyInfo
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public FontFamily FontFamily { get; set; } = null!;
}

public sealed class FontWeightInfo
{
    public string DisplayName { get; set; } = null!;
    public FontWeight FontWeight { get; set; }
}
