using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Models;
using Windows.Foundation;

namespace The_Untamed_Music_Player.ViewModels;
public class DesktopLyricViewModel
{

    public DesktopLyricViewModel()
    {
    }

    public void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Data.DesktopLyricWindow?.Close();
        Data.DesktopLyricWindow?.Dispose();
    }

    public double GetTextBlockWidth(string currentLyricContent)
    {
        if (currentLyricContent == "")
        {
            return 100;
        }
        var textBlock = new TextBlock
        {
            Text = currentLyricContent,
            FontFamily = Data.SettingsViewModel?.SelectedFont,
            FontSize = 32
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return textBlock.DesiredSize.Width;
    }

    public double GetTextBlockHeight(string currentLyricContent)
    {
        var textBlock = new TextBlock
        {
            Text = currentLyricContent,
            FontFamily = Data.SettingsViewModel?.SelectedFont,
            FontSize = 32
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return textBlock.DesiredSize.Height;
    }

    public double GetBorderWidth(double textBlockWidth)
    {
        return textBlockWidth + 50;
    }

    public double GetBorderHeight(double textBlockHeight)
    {
        return textBlockHeight + 20;
    }
}
