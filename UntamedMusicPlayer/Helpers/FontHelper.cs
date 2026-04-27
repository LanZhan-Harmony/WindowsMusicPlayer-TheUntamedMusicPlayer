using System.Globalization;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using UntamedMusicPlayer.Models;
using Windows.UI.Text;
using ZLinq;

namespace UntamedMusicPlayer.Helpers;

public static class FontHelper
{
    private static List<FontFamilyInfo>? _systemFontFamilies;
    private static List<FontWeightInfo>? _FontWeights;

    public static List<FontFamilyInfo> GetSystemFontFamilies()
    {
        if (_systemFontFamilies is not null)
        {
            return _systemFontFamilies;
        }
        var language = new string[] { CultureInfo.CurrentUICulture.Name.ToLowerInvariant() };
        var names = CanvasTextFormat.GetSystemFontFamilies();
        var displayNames = CanvasTextFormat.GetSystemFontFamilies(language);
        var list = new List<FontFamilyInfo>();
        for (var i = 0; i < names.Length; i++)
        {
            list.Add(
                new FontFamilyInfo
                {
                    Name = names[i],
                    DisplayName = displayNames[i],
                    FontFamily = new FontFamily(names[i]),
                }
            );
        }
        _systemFontFamilies = [.. list.AsValueEnumerable().OrderBy(f => f.Name)];
        return _systemFontFamilies;
    }

    public static List<FontWeightInfo> GetFontWeights()
    {
        if (_FontWeights is not null)
        {
            return _FontWeights;
        }
        var names = "Settings_FontWeights".GetLocalized().Split(", ");
        _FontWeights =
        [
            new() { DisplayName = names[0], FontWeight = FontWeights.Thin },
            new() { DisplayName = names[1], FontWeight = FontWeights.ExtraLight },
            new() { DisplayName = names[2], FontWeight = FontWeights.Light },
            new() { DisplayName = names[3], FontWeight = FontWeights.SemiLight },
            new() { DisplayName = names[4], FontWeight = FontWeights.Normal },
            new() { DisplayName = names[5], FontWeight = FontWeights.Medium },
            new() { DisplayName = names[6], FontWeight = FontWeights.SemiBold },
            new() { DisplayName = names[7], FontWeight = FontWeights.Bold },
            new() { DisplayName = names[8], FontWeight = FontWeights.ExtraBold },
            new() { DisplayName = names[9], FontWeight = FontWeights.Black },
            new() { DisplayName = names[10], FontWeight = FontWeights.ExtraBlack },
        ];
        return _FontWeights;
    }

    public static FontWeight ConvertToFontWeight(ushort weight)
    {
        return weight switch
        {
            100 => FontWeights.Thin,
            200 => FontWeights.ExtraLight,
            300 => FontWeights.Light,
            350 => FontWeights.SemiLight,
            400 => FontWeights.Normal,
            500 => FontWeights.Medium,
            600 => FontWeights.SemiBold,
            700 => FontWeights.Bold,
            800 => FontWeights.ExtraBold,
            900 => FontWeights.Black,
            950 => FontWeights.ExtraBlack,
            _ => FontWeights.Normal,
        };
    }
}
