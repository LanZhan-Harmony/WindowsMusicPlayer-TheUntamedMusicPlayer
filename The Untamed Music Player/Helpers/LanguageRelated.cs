using Microsoft.UI.Text;
using The_Untamed_Music_Player.Models;
using Windows.UI.Text;

namespace The_Untamed_Music_Player.Helpers;

public class LanguageRelated
{
    public static FontWeight GetTitleFontWeight()
    {
        return Data.Language switch
        {
            "简体中文" => FontWeights.SemiLight,
            "English" => FontWeights.SemiBold,
            _ => FontWeights.SemiLight,
        };
    }
}
