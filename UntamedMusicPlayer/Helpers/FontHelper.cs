using System.Globalization;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
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
            list.Add(new FontFamilyInfo { Name = names[i], DisplayName = displayNames[i] });
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
            new() { DisplayName = names[0], Weight = FontWeights.Thin.Weight },
            new() { DisplayName = names[1], Weight = FontWeights.ExtraLight.Weight },
            new() { DisplayName = names[2], Weight = FontWeights.Light.Weight },
            new() { DisplayName = names[3], Weight = FontWeights.SemiLight.Weight },
            new() { DisplayName = names[4], Weight = FontWeights.Normal.Weight },
            new() { DisplayName = names[5], Weight = FontWeights.Medium.Weight },
            new() { DisplayName = names[6], Weight = FontWeights.SemiBold.Weight },
            new() { DisplayName = names[7], Weight = FontWeights.Bold.Weight },
            new() { DisplayName = names[8], Weight = FontWeights.ExtraBold.Weight },
            new() { DisplayName = names[9], Weight = FontWeights.Black.Weight },
            new() { DisplayName = names[10], Weight = FontWeights.ExtraBlack.Weight },
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
