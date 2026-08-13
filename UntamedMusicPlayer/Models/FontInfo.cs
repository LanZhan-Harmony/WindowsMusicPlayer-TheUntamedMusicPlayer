using Microsoft.UI.Xaml.Media;
using UntamedMusicPlayer.Helpers;
using Windows.UI.Text;

namespace UntamedMusicPlayer.Models;

public sealed class FontFamilyInfo
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public FontFamily FontFamily => new(Name);
}

public sealed class FontWeightInfo
{
    public string DisplayName { get; set; } = null!;
    public ushort Weight { get; set; }
    public FontWeight FontWeight => FontHelper.ConvertToFontWeight(Weight);
}
