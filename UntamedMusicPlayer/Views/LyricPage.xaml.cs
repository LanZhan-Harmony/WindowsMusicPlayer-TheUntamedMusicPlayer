using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using Windows.Foundation;

namespace UntamedMusicPlayer.Views;

public sealed partial class LyricPage : Page, IDisposable
{
    public LyricViewModel ViewModel { get; }

    private readonly Timer _autoScrollDelayTimer;
    private bool _isManualScrolling;
    private bool _isProgrammaticScroll;

    private readonly Timer _titleBarTimer;
    private bool _titleBarTimerEnabled = false;
    private bool _isTitleBarHidden = false;

    public LyricPage()
    {
        ViewModel = App.GetService<LyricViewModel>();
        InitializeComponent();

        Data.PlayState.PropertyChanged += OnStateChanged;

        _autoScrollDelayTimer = new Timer(
            _ => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ScrollToCurrentLyric),
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );

        _titleBarTimer = new Timer(TimerTick, null, Timeout.Infinite, Timeout.Infinite);

        Data.RootPlayBarViewModel?.PropertyChanged += RootPlayBarViewModelPropertyChanged;
    }

    private void RootPlayBarViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is nameof(RootPlayBarViewModel.IsDetail)
                or nameof(RootPlayBarViewModel.IsFullScreen)
        )
        {
            if (Data.RootPlayBarViewModel!.IsDetail && Data.RootPlayBarViewModel.IsFullScreen)
            {
                RootGrid.PointerMoved += RootGrid_PointerMoved;
            }
            else
            {
                RootGrid.PointerMoved -= RootGrid_PointerMoved;
                if (_isTitleBarHidden)
                {
                    ContentGrid.Margin = new Thickness(0);
                    ShowTitleBarStoryboard.Begin();
                    _isTitleBarHidden = false;
                    StopTitleBarTimer();
                }
            }
        }
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(RootGrid);
        var position = pointerPoint.Position;

        if (position.Y < 33) // 如果鼠标在顶部 33 像素范围内
        {
            if (_isTitleBarHidden)
            {
                ContentGrid.Margin = new Thickness(0);
                ShowTitleBarStoryboard.Begin();
                _isTitleBarHidden = false;
            }
            StopTitleBarTimer();
        }
        else if (!_isTitleBarHidden) // 鼠标离开了底部，开始定时器
        {
            StartTitleBarTimer();
        }
    }

    private void TimerTick(object? state)
    {
        StopTitleBarTimer();
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    Data.RootPlayBarViewModel!.IsDetail
                    && Data.RootPlayBarViewModel.IsFullScreen
                    && !_isTitleBarHidden
                )
                {
                    ContentGrid.Margin = new Thickness(0, -33, 0, 0);
                    HideTitleBarStoryboard.Begin();
                    _isTitleBarHidden = true;
                }
            }
        );
    }

    private void StartTitleBarTimer()
    {
        if (!_titleBarTimerEnabled)
        {
            _titleBarTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
            _titleBarTimerEnabled = true;
        }
    }

    private void StopTitleBarTimer()
    {
        if (_titleBarTimerEnabled)
        {
            _titleBarTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _titleBarTimerEnabled = false;
        }
    }

    private void CoverBtnClickToDetail(object sender, RoutedEventArgs e)
    {
        Data.RootPlayBarViewModel!.DetailModeUpdate();
    }

    private void AddToSubItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutSubItem menuItem)
        {
            while (menuItem.Items.Count > 3)
            {
                menuItem.Items.RemoveAt(3);
            }
            foreach (var playlist in Data.PlaylistLibrary.Playlists)
            {
                var playlistMenuItem = new MenuFlyoutItem
                {
                    Text = playlist.Name,
                    DataContext = playlist,
                };
                playlistMenuItem.Click += PlaylistMenuItem_Click;
                menuItem.Items.Add(playlistMenuItem);
            }
        }
    }

    private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: PlaylistInfo playlist })
        {
            ViewModel.AddToPlaylistButton_Click(playlist);
        }
    }

    private async void AddToNewPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewPlaylistInfoDialog() { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.CreatedPlaylist is not null)
        {
            ViewModel.AddToPlaylistButton_Click(dialog.CreatedPlaylist);
        }
    }

    private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
    {
        var currentSong = Data.PlayState.CurrentSong;
        var dialog = new PropertiesDialog(currentSong!) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SharedPlaybackState.CurrentSong))
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

        var currentCover = Data.PlayState.CurrentSong?.Cover;
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
        if (_isManualScrolling)
        {
            return;
        }

        var textblock = (sender as TextBlock)!;
        if (Math.Abs(textblock.FontSize - Settings.LyricPageCurrentFontSize) < 1e-3)
        {
            var currentScrollPosition = LyricViewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = textblock.TransformToVisual(LyricViewer).TransformPoint(point);

            _isProgrammaticScroll = true;
            LyricViewer.ChangeView(
                null,
                targetPosition.Y - LyricViewer.ActualHeight / 2 + 40,
                null,
                false
            );
        }
    }

    private void LyricViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isProgrammaticScroll) // 如果是自动滚动
        {
            if (!e.IsIntermediate) // 自动滚动完成后重置标志
            {
                _isProgrammaticScroll = false;
            }
            return;
        }

        _isManualScrolling = true;
        _autoScrollDelayTimer.Change(Timeout.Infinite, Timeout.Infinite);
        if (!e.IsIntermediate) // 用户停止滚动，启动5秒倒计时
        {
            _autoScrollDelayTimer.Change(5000, Timeout.Infinite);
        }
    }

    private void ScrollToCurrentLyric()
    {
        _isManualScrolling = false;
        var currentSlice = Data.LyricManager.CurrentLyricSlices.FirstOrDefault(s => s.IsCurrent);
        if (
            currentSlice is null
            || LyricView.ContainerFromItem(currentSlice) is not UIElement container
        )
        {
            return;
        }

        var currentScrollPosition = LyricViewer.VerticalOffset;
        var point = new Point(0, currentScrollPosition);

        // 获取容器相对于 ScrollViewer 的位置
        // 容器位置 + 歌词顶部的 Margin (40) 约等于 TextBlock 的位置
        var targetPosition = container.TransformToVisual(LyricViewer).TransformPoint(point);

        _isProgrammaticScroll = true;
        LyricViewer.ChangeView(
            null,
            targetPosition.Y - LyricViewer.ActualHeight / 2 + 80, // 40 (Margin) + 40 (Offset)
            null,
            false
        );
    }

    public void Dispose()
    {
        _autoScrollDelayTimer.Dispose();
        Data.PlayState.PropertyChanged -= OnStateChanged;
        Data.LyricPage = null;
    }
}
