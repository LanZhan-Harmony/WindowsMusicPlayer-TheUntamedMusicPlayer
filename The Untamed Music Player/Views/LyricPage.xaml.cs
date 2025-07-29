using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;
using The_Untamed_Music_Player.ViewModels;
using Windows.Foundation;

namespace The_Untamed_Music_Player.Views;

public sealed partial class LyricPage : Page, IDisposable
{
    public LyricViewModel ViewModel { get; }

    public LyricPage()
    {
        ViewModel = App.GetService<LyricViewModel>();
        InitializeComponent();

        Data.MusicPlayer.PropertyChanged += MusicPlayer_PropertyChanged;

        // 设置 ContentGridBackground 的绑定
        /*var contentGridBinding = new Binding
        {
            Path = new PropertyPath("CurrentSong.Cover"),
            Source = Data.MusicPlayer,
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(ContentGridBackground, ImageBrush.ImageSourceProperty, contentGridBinding);*/

        /*var isLyricBackgroundVisible = Data.IsLyricBackgroundVisible;
        if (!isLyricBackgroundVisible)
        {
            ContentGridBackground.Opacity = 0;
        }
        else
        {
            var acrylicBrush = new AcrylicBrush { TintOpacity = 0.8 };

            var isDarkTheme =
                ((FrameworkElement)App.MainWindow!.Content).ActualTheme == ElementTheme.Dark
                || (
                    ((FrameworkElement)App.MainWindow.Content).ActualTheme == ElementTheme.Default
                    && App.Current.RequestedTheme == ApplicationTheme.Dark
                );

            if (isDarkTheme)
            {
                acrylicBrush.TintColor = Colors.Black;
            }
            else
            {
                acrylicBrush.TintColor = Colors.White;
            }
            SecondaryContentGrid.Background = acrylicBrush;
        }*/
    }

    private void MusicPlayer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Data.MusicPlayer.CurrentSong))
        {
            if (ReferenceGrid.ActualWidth > 0 && ReferenceGrid.ActualHeight > 0)
            {
                ChangeCoverSize(ReferenceGrid.ActualWidth, ReferenceGrid.ActualHeight);
            }
        }
    }

    private void ReferenceGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ChangeCoverSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void ChangeCoverSize(double width, double height)
    {
        var baseNum = Math.Min(width, height);
        var scalingMargin = baseNum < 300 ? 50 : baseNum * 0.4;

        var availableWidth = width - scalingMargin;
        var availableHeight = height - scalingMargin;

        double coverWidth,
            coverHeight;

        var currentCover = Data.MusicPlayer.CurrentSong?.Cover;

        if (currentCover?.PixelWidth > 0 && currentCover?.PixelHeight > 0)
        {
            // 计算封面的宽高比
            var aspectRatio = (double)currentCover.PixelWidth / currentCover.PixelHeight;

            // 根据可用空间和宽高比计算最佳尺寸
            if (aspectRatio > 1) // 宽图
            {
                coverWidth = Math.Min(availableWidth, availableHeight * aspectRatio);
                coverHeight = coverWidth / aspectRatio;
            }
            else // 高图或正方形
            {
                coverHeight = Math.Min(availableHeight, availableWidth / aspectRatio);
                coverWidth = coverHeight * aspectRatio;
            }

            // 确保不超出可用空间
            if (coverWidth > availableWidth)
            {
                coverWidth = availableWidth;
                coverHeight = coverWidth / aspectRatio;
            }
            if (coverHeight > availableHeight)
            {
                coverHeight = availableHeight;
                coverWidth = coverHeight * aspectRatio;
            }
        }
        else
        {
            // 如果没有封面或无法获取尺寸，使用正方形
            var length = Math.Min(availableWidth, availableHeight);
            coverWidth = coverHeight = length;
        }

        CoverBorder.Width = coverWidth;
        CoverBorder.Height = coverHeight;
    }

    private void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var textblock = (TextBlock)sender;
        if (textblock.FontSize == 50 || textblock.FontSize == 24)
        {
            var currentScrollPosition = LyricViewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = textblock.TransformToVisual(LyricViewer).TransformPoint(point);

            LyricViewer.ChangeView(
                null,
                targetPosition.Y - LyricViewer.ActualHeight / 2 + 40,
                null,
                disableAnimation: false
            );
        }
    }

    public void Dispose() { }
}
