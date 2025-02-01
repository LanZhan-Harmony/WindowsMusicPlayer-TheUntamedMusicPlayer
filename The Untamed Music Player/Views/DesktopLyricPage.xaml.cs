using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using The_Untamed_Music_Player.Models;
using Windows.Foundation;

namespace The_Untamed_Music_Player.Views;
public sealed partial class DesktopLyricPage : Page
{
    public DesktopLyricPage()
    {
        InitializeComponent();
    }

    private double GetTextBlockWidth(string currentLyricContent)
    {
        if (currentLyricContent == "")
        {
            return 100;
        }
        var textBlock = new TextBlock
        {
            Text = currentLyricContent,
            FontFamily = Data.SettingsViewModel!.SelectedFont,
            FontSize = 32
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var textBlockWidth = textBlock.DesiredSize.Width;
        return textBlockWidth <= 620 ? textBlockWidth : 620;
    }

    private double GetTextBlockHeight(string currentLyricContent)
    {
        var textBlock = new TextBlock
        {
            Text = currentLyricContent,
            FontFamily = Data.SettingsViewModel!.SelectedFont,
            FontSize = 32
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return textBlock.DesiredSize.Height;
    }

    private double GetBorderHeight(double textBlockHeight)
    {
        return textBlockHeight + 20;
    }

    private void LyricContentTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            var widthAnimation = new DoubleAnimation
            {
                From = e.PreviousSize.Width + 50,
                To = e.NewSize.Width + 50,
                Duration = TimeSpan.FromMilliseconds(300),
                EnableDependentAnimation = true,
                EasingFunction = new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = 1// Õñ·ù
                }
            };
            Storyboard.SetTarget(widthAnimation, AnimatedBorder);
            Storyboard.SetTargetProperty(widthAnimation, "Width");
            var storyboard = new Storyboard();
            storyboard.Children.Add(widthAnimation);
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
