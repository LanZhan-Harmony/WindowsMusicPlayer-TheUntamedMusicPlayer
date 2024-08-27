using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace The_Untamed_Music_Player.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class 歌词Page : Page
{
    public 歌词ViewModel ViewModel
    {
        get;
    }

    public 歌词Page()
    {
        ViewModel = App.GetService<歌词ViewModel>();
        InitializeComponent();
        MusicPlayer.歌词UI = this;

        // 设置 ContentGridBackground 的绑定
        /*var contentGridBinding = new Binding
        {
            Path = new PropertyPath("CurrentMusic.Cover"),
            Source = Data.MusicPlayer,
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(ContentGridBackground, ImageBrush.ImageSourceProperty, contentGridBinding);*/

        var isLyricBackgroundVisible = Data.SettingsViewModel?.IsLyricBackgroundVisible;
        if (isLyricBackgroundVisible == false)
        {
            ContentGridBackground.Opacity = 0;
        }
        else
        {
            var acrylicBrush = new AcrylicBrush
            {
                TintOpacity = 0.8,
            };
            var currentTheme = Data.SettingsViewModel?.ElementTheme;
            if (currentTheme == ElementTheme.Dark)
            {
                acrylicBrush.TintColor = Colors.Black;
            }
            else
            {
                acrylicBrush.TintColor = Colors.White;
            }
            SecondaryContentGrid.Background = acrylicBrush;
        }
    }

    private void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var textblock = (TextBlock)sender;
        if (Data.MainWindow != null)
        {
            try
            {
                LyricViewer.Height = Data.MainWindow.Height - 117 - 40;
            }
            catch//切换到后台
            {
                LyricViewer.Height = 0;
            }
        }
        else
        {
            LyricViewer.Height = 0;
        }
        if (textblock.FontSize == 50 || textblock.FontSize == 24)
        {
            var currentScrollPosition = LyricViewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = textblock.TransformToVisual(LyricViewer).TransformPoint(point);
            if (textblock.FontSize == 24)
            {
                LyricViewer.ChangeView(null, targetPosition.Y, null, disableAnimation: false);
            }
            else
            {
                LyricViewer.ChangeView(null, targetPosition.Y - LyricViewer.Height / 2 + 40, null, disableAnimation: false);
            }
        }
    }
}
