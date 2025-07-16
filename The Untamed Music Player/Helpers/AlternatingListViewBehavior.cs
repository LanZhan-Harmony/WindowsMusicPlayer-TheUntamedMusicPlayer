using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace The_Untamed_Music_Player.Helpers;

public class AlternatingListViewBehavior
{
    public static Brush GetAlternateBackgroundBrush(bool isDarkTheme)
    {
        return isDarkTheme
            ? new SolidColorBrush(Color.FromArgb(170, 48, 53, 57))
            : new SolidColorBrush(Color.FromArgb(170, 253, 254, 254));
    }
}
