using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    }

    private void CoverBtnClickToDetail(object sender, RoutedEventArgs e)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
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
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // 计算可用空间
        var baseNum = Math.Min(width, height);
        var scalingMargin = baseNum < 300 ? 50 : baseNum * 0.4;
        var availableWidth = Math.Max(0, width - scalingMargin);
        var availableHeight = Math.Max(0, height - scalingMargin);

        var currentCover = Data.MusicPlayer.CurrentSong?.Cover;
        double coverWidth,
            coverHeight;

        if (currentCover?.PixelWidth > 0 && currentCover?.PixelHeight > 0)
        {
            var aspectRatio = (double)currentCover.PixelWidth / currentCover.PixelHeight;

            var widthBasedHeight = availableWidth / aspectRatio;
            var heightBasedWidth = availableHeight * aspectRatio;

            (coverWidth, coverHeight) =
                widthBasedHeight <= availableHeight
                    ? (availableWidth, widthBasedHeight)
                    : (heightBasedWidth, availableHeight);
        }
        else // 默认正方形
        {
            coverWidth = coverHeight = Math.Min(availableWidth, availableHeight);
        }

        CoverBorder.Width = coverWidth;
        CoverBorder.Height = coverHeight;
    }

    private void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var textblock = sender as TextBlock;
        if (textblock!.FontSize == Data.SelectedCurrentFontSize)
        {
            var currentScrollPosition = LyricViewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = textblock.TransformToVisual(LyricViewer).TransformPoint(point);

            LyricViewer.ChangeView(
                null,
                targetPosition.Y - LyricViewer.ActualHeight / 2 + 40,
                null,
                false
            );
        }
    }

    public void Dispose()
    {
        Data.MusicPlayer.PropertyChanged -= MusicPlayer_PropertyChanged;
    }
}
