using System.ComponentModel;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedMusicPlayer.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Playback;
using UntamedMusicPlayer.ViewModels;
using Windows.Foundation;

namespace UntamedMusicPlayer.Views;

public sealed partial class LyricPage : Page, IDisposable
{
    private enum LyricAdjustBorderAnimationState
    {
        Hidden,
        Showing,
        Visible,
        Hiding,
    }

    public LyricViewModel ViewModel { get; }

    private bool isFirstLoad = true;

    private readonly Timer _autoScrollDelayTimer;
    private bool _isManualScrolling;
    private bool _isProgrammaticScroll;
    private double? _programmaticTargetOffset;
    private DispatcherQueueTimer? _programmaticScrollFallbackTimer;

    private Timer? _titleBarHideTimer;
    private bool _titleBarTimerEnabled = false;
    private bool _isTitleBarHidden = false;

    private DispatcherQueueTimer? _contentGridMarginAnimationTimer;
    private DateTimeOffset _contentGridMarginAnimationStart;
    private double _contentGridMarginFrom;
    private double _contentGridMarginTo;
    private double _contentGridMarginAnimationDuration;

    private CancellationTokenSource? _coverLoadWaitCts;
    private CancellationTokenSource? _ensureLyricScrollCts;

    private bool _isLyricViewLoaded;

    private Visual? _lyricAdjustBorderVisual;
    private LyricAdjustBorderAnimationState _lyricAdjustBorderAnimationState =
        LyricAdjustBorderAnimationState.Hidden;
    private bool _lyricAdjustShouldBeVisible;
    private int _lyricAdjustAnimationVersion;

    public LyricPage()
    {
        ViewModel = App.GetService<LyricViewModel>();
        InitializeComponent();

        _autoScrollDelayTimer = new Timer(
            _ => OnAutoScrollDelayTimerTick(),
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );

        Data.PlayState.PropertyChanged += OnStateChanged;
        Data.RootPlayBarViewModel?.PropertyChanged += OnRootPlayBarChanged;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SharedPlaybackState.CurrentSong))
        {
            if (ReferenceGrid.ActualWidth > 0 && ReferenceGrid.ActualHeight > 0)
            {
                RestartWaitForCoverAndRecalculate();
            }
            _isManualScrolling = false;
            _autoScrollDelayTimer.Change(Timeout.Infinite, Timeout.Infinite);
            RestartEnsureScrollToCurrentLyric();
        }
    }

    private void LyricView_Loaded(object sender, RoutedEventArgs e)
    {
        _isLyricViewLoaded = true;
        RestartEnsureScrollToCurrentLyric();
    }

    private void RestartEnsureScrollToCurrentLyric()
    {
        _ensureLyricScrollCts?.Cancel();
        _ensureLyricScrollCts?.Dispose();
        _ensureLyricScrollCts = new CancellationTokenSource();
        _ = EnsureScrollToCurrentLyricWhenReadyAsync(_ensureLyricScrollCts.Token);
    }

    private async Task EnsureScrollToCurrentLyricWhenReadyAsync(CancellationToken cancellationToken)
    {
        const int maxRetryCount = 10;
        for (var i = 0; i < maxRetryCount; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (await TryScrollToCurrentLyricOnUiThreadAsync(cancellationToken))
            {
                try
                {
                    await Task.Delay(120, cancellationToken);
                    await TryScrollToCurrentLyricOnUiThreadAsync(cancellationToken);
                }
                catch (OperationCanceledException) { }
                return;
            }
            try
            {
                await Task.Delay(200, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private Task<bool> TryScrollToCurrentLyricOnUiThreadAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var enqueued = DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetResult(false);
                    return;
                }
                tcs.TrySetResult(TryScrollToCurrentLyric());
            }
        );

        if (!enqueued)
        {
            tcs.TrySetResult(false);
        }
        return tcs.Task;
    }

    private void RestartWaitForCoverAndRecalculate()
    {
        _coverLoadWaitCts?.Cancel();
        _coverLoadWaitCts?.Dispose();
        _coverLoadWaitCts = new CancellationTokenSource();
        _ = RecalculateCoverSizeWhenCoverReadyAsync(_coverLoadWaitCts.Token);
    }

    private async Task RecalculateCoverSizeWhenCoverReadyAsync(CancellationToken cancellationToken)
    {
        var cover = Data.PlayState.CurrentSong?.Cover;
        if (cover is null)
        {
            return;
        }

        var loaded = await WaitCoverLoadedAsync(cover, cancellationToken);
        if (!loaded || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (ReferenceGrid.ActualWidth > 0 && ReferenceGrid.ActualHeight > 0)
        {
            ChangeCoverSize(ReferenceGrid.ActualWidth, ReferenceGrid.ActualHeight);
        }
    }

    private static async Task<bool> WaitCoverLoadedAsync(
        BitmapImage cover,
        CancellationToken cancellationToken
    )
    {
        if (cover.PixelWidth > 0 && cover.PixelHeight > 0)
        {
            return true;
        }

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void OnImageOpened(object sender, RoutedEventArgs args) => tcs.TrySetResult(true);
        void OnImageFailed(object sender, ExceptionRoutedEventArgs args) => tcs.TrySetResult(false);

        cover.ImageOpened += OnImageOpened;
        cover.ImageFailed += OnImageFailed;

        using var cancellationRegistration = cancellationToken.Register(() =>
            tcs.TrySetCanceled(cancellationToken)
        );

        try
        {
            if (cover.PixelWidth > 0 && cover.PixelHeight > 0)
            {
                return true;
            }
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1500, cancellationToken));
            if (completedTask != tcs.Task)
            {
                return false;
            }
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            cover.ImageOpened -= OnImageOpened;
            cover.ImageFailed -= OnImageFailed;
        }
    }

    private void OnRootPlayBarChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            Settings.IsAutoHidePlaybackControlBar
            && e.PropertyName
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
                    AnimateContentGridTopMargin(0, 300);
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
                AnimateContentGridTopMargin(0, 300);
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

    private void TitleBarHideTimerTick(object? state)
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
                    AnimateContentGridTopMargin(-33, 600);
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
            _titleBarHideTimer ??= new Timer(
                TitleBarHideTimerTick,
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );
            _titleBarHideTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
            _titleBarTimerEnabled = true;
        }
    }

    private void StopTitleBarTimer()
    {
        if (_titleBarTimerEnabled)
        {
            _titleBarHideTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _titleBarTimerEnabled = false;
        }
    }

    private void AnimateContentGridTopMargin(double targetTop, double durationMs)
    {
        var currentTop = ContentGrid.Margin.Top;
        if (Math.Abs(currentTop - targetTop) < 0.1)
        {
            ContentGrid.Margin = new Thickness(0, targetTop, 0, 0);
            return;
        }

        _contentGridMarginAnimationTimer ??= DispatcherQueue.CreateTimer();
        _contentGridMarginAnimationTimer.Stop();
        _contentGridMarginAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _contentGridMarginAnimationTimer.Tick -= ContentGridMarginAnimationTick;
        _contentGridMarginAnimationTimer.Tick += ContentGridMarginAnimationTick;

        _contentGridMarginFrom = currentTop;
        _contentGridMarginTo = targetTop;
        _contentGridMarginAnimationDuration = durationMs;
        _contentGridMarginAnimationStart = DateTimeOffset.Now;

        _contentGridMarginAnimationTimer.Start();
    }

    private void ContentGridMarginAnimationTick(DispatcherQueueTimer sender, object args)
    {
        var elapsedMs = (DateTimeOffset.Now - _contentGridMarginAnimationStart).TotalMilliseconds;
        var progress = Math.Clamp(elapsedMs / _contentGridMarginAnimationDuration, 0d, 1d);

        // 显示 (To > From): EaseOut
        // 隐藏 (To < From): EaseIn
        var easedProgress =
            _contentGridMarginTo > _contentGridMarginFrom
                ? 1 - Math.Pow(1 - progress, 3)
                : Math.Pow(progress, 3);

        var currentTop =
            _contentGridMarginFrom
            + ((_contentGridMarginTo - _contentGridMarginFrom) * easedProgress);
        ContentGrid.Margin = new Thickness(0, currentTop, 0, 0);

        if (progress >= 1)
        {
            sender.Stop();
            sender.Tick -= ContentGridMarginAnimationTick;
            ContentGrid.Margin = new Thickness(0, _contentGridMarginTo, 0, 0);
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

        AnimateCoverSize(coverWidth, coverHeight);
    }

    private void AnimateCoverSize(double targetWidth, double targetHeight)
    {
        var currentWidth = double.IsNaN(CoverBorder.Width)
            ? CoverBorder.ActualWidth
            : CoverBorder.Width;
        var currentHeight = double.IsNaN(CoverBorder.Height)
            ? CoverBorder.ActualHeight
            : CoverBorder.Height;

        if (currentWidth <= 0 || currentHeight <= 0)
        {
            CoverBorder.Width = targetWidth;
            CoverBorder.Height = targetHeight;
            return;
        }

        if (isFirstLoad)
        {
            isFirstLoad = false;
            CoverBorder.Width = targetWidth;
            CoverBorder.Height = targetHeight;
            return;
        }

        if (
            Math.Abs(currentWidth - targetWidth) < 0.5
            && Math.Abs(currentHeight - targetHeight) < 0.5
        )
        {
            CoverBorder.Width = targetWidth;
            CoverBorder.Height = targetHeight;
            return;
        }

        CoverBorder.Width = targetWidth;
        CoverBorder.Height = targetHeight;

        var visual = ElementCompositionPreview.GetElementVisual(CoverBorder);
        visual.StopAnimation("Scale");
        visual.CenterPoint = new Vector3((float)(targetWidth / 2), (float)(targetHeight / 2), 0f);

        var initialScaleX = (float)(currentWidth / targetWidth);
        var initialScaleY = (float)(currentHeight / targetHeight);
        visual.Scale = new Vector3(initialScaleX, initialScaleY, 1f);

        var compositor = visual.Compositor;

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1f, Vector3.One);
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(450);

        visual.StartAnimation("Scale", scaleAnimation);
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
            var topInViewport = textblock
                .TransformToVisual(LyricViewer)
                .TransformPoint(new Point(0, 0))
                .Y;
            var targetOffset =
                LyricViewer.VerticalOffset + topInViewport - LyricViewer.ActualHeight / 2 + 40;
            targetOffset = Math.Max(0, targetOffset);

            MarkProgrammaticScroll(targetOffset);
            LyricViewer.ChangeView(null, targetOffset, null, false);
        }
    }

    private void LyricViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_isProgrammaticScroll)
        {
            ClearProgrammaticScrollState();
        }
    }

    private void LyricViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isProgrammaticScroll) // 如果是自动滚动
        {
            if (
                !e.IsIntermediate
                || (
                    _programmaticTargetOffset.HasValue
                    && Math.Abs(LyricViewer.VerticalOffset - _programmaticTargetOffset.Value) < 1.0
                )
            )
            {
                ClearProgrammaticScrollState();
            }
            return;
        }

        var wasManualScrolling = _isManualScrolling;
        _isManualScrolling = true;
        if (!wasManualScrolling)
        {
            ShowLyricAdjustBorder();
        }
        _autoScrollDelayTimer.Change(Timeout.Infinite, Timeout.Infinite);
        if (!e.IsIntermediate) // 用户停止滚动，启动3秒倒计时
        {
            _autoScrollDelayTimer.Change(3000, Timeout.Infinite);
        }
    }

    private void OnAutoScrollDelayTimerTick()
    {
        _isManualScrolling = false;
        RestartEnsureScrollToCurrentLyric();
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, HideLyricAdjustBorder);
    }

    private void EnsureLyricAdjustBorderVisualInitialized()
    {
        if (_lyricAdjustBorderVisual is not null)
        {
            return;
        }

        _lyricAdjustBorderVisual = ElementCompositionPreview.GetElementVisual(LyricAdjustBorder);
        ElementCompositionPreview.SetIsTranslationEnabled(LyricAdjustBorder, true);
        var hiddenOffsetX = (float)GetLyricAdjustBorderHiddenOffset();
        LyricAdjustBorder.Translation = new Vector3(hiddenOffsetX, 0f, 0f);
        _lyricAdjustBorderVisual.Opacity = 0f;
    }

    private double GetLyricAdjustBorderHiddenOffset()
    {
        var width = LyricAdjustBorder.ActualWidth;
        if (width <= 0)
        {
            width = 48;
        }
        return width + LyricAdjustBorder.Margin.Right + 24;
    }

    private void ShowLyricAdjustBorder()
    {
        EnsureLyricAdjustBorderVisualInitialized();
        if (_lyricAdjustBorderVisual is null)
        {
            return;
        }

        _lyricAdjustShouldBeVisible = true;
        if (
            _lyricAdjustBorderAnimationState
            is LyricAdjustBorderAnimationState.Visible or LyricAdjustBorderAnimationState.Showing
        )
        {
            return;
        }

        var previousState = _lyricAdjustBorderAnimationState;
        var animationVersion = ++_lyricAdjustAnimationVersion;
        _lyricAdjustBorderAnimationState = LyricAdjustBorderAnimationState.Showing;

        LyricAdjustBorder.Visibility = Visibility.Visible;

        var hiddenOffsetX = (float)GetLyricAdjustBorderHiddenOffset();
        if (previousState == LyricAdjustBorderAnimationState.Hidden)
        {
            LyricAdjustBorder.Translation = new Vector3(hiddenOffsetX, 0f, 0f);
            _lyricAdjustBorderVisual.Opacity = 0f;
        }

        _lyricAdjustBorderVisual.StopAnimation("Translation.X");
        _lyricAdjustBorderVisual.StopAnimation("Opacity");

        var compositor = _lyricAdjustBorderVisual.Compositor;

        var slideIn = compositor.CreateScalarKeyFrameAnimation();
        slideIn.InsertKeyFrame(
            1f,
            0f,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.2f, 1f))
        );
        slideIn.Duration = TimeSpan.FromMilliseconds(280);

        var fadeIn = compositor.CreateScalarKeyFrameAnimation();
        fadeIn.InsertKeyFrame(
            1f,
            1f,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.2f, 1f))
        );
        fadeIn.Duration = TimeSpan.FromMilliseconds(220);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) =>
        {
            if (animationVersion != _lyricAdjustAnimationVersion)
            {
                return;
            }

            if (_lyricAdjustShouldBeVisible)
            {
                _lyricAdjustBorderAnimationState = LyricAdjustBorderAnimationState.Visible;
                LyricAdjustBorder.Translation = Vector3.Zero;
                _lyricAdjustBorderVisual.Opacity = 1f;
            }
        };

        _lyricAdjustBorderVisual.StartAnimation("Translation.X", slideIn);
        _lyricAdjustBorderVisual.StartAnimation("Opacity", fadeIn);
        batch.End();
    }

    private void HideLyricAdjustBorder()
    {
        EnsureLyricAdjustBorderVisualInitialized();
        if (_lyricAdjustBorderVisual is null)
        {
            return;
        }

        _lyricAdjustShouldBeVisible = false;
        if (
            _lyricAdjustBorderAnimationState
            is LyricAdjustBorderAnimationState.Hidden or LyricAdjustBorderAnimationState.Hiding
        )
        {
            return;
        }

        _lyricAdjustBorderAnimationState = LyricAdjustBorderAnimationState.Hiding;
        var animationVersion = ++_lyricAdjustAnimationVersion;

        _lyricAdjustBorderVisual.StopAnimation("Translation.X");
        _lyricAdjustBorderVisual.StopAnimation("Opacity");

        var compositor = _lyricAdjustBorderVisual.Compositor;
        var hiddenOffsetX = (float)GetLyricAdjustBorderHiddenOffset();

        var slideOut = compositor.CreateScalarKeyFrameAnimation();
        slideOut.InsertKeyFrame(
            1f,
            hiddenOffsetX,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(1f, 1f))
        );
        slideOut.Duration = TimeSpan.FromMilliseconds(240);

        var fadeOut = compositor.CreateScalarKeyFrameAnimation();
        fadeOut.InsertKeyFrame(
            1f,
            0f,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(1f, 1f))
        );
        fadeOut.Duration = TimeSpan.FromMilliseconds(180);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        batch.Completed += (_, _) =>
        {
            if (animationVersion != _lyricAdjustAnimationVersion)
            {
                return;
            }

            if (!_lyricAdjustShouldBeVisible)
            {
                LyricAdjustBorder.Visibility = Visibility.Collapsed;
                LyricAdjustBorder.Translation = new Vector3(hiddenOffsetX, 0f, 0f);
                _lyricAdjustBorderVisual.Opacity = 0f;
                _lyricAdjustBorderAnimationState = LyricAdjustBorderAnimationState.Hidden;
            }
        };

        _lyricAdjustBorderVisual.StartAnimation("Translation.X", slideOut);
        _lyricAdjustBorderVisual.StartAnimation("Opacity", fadeOut);
        batch.End();
    }

    private bool TryScrollToCurrentLyric()
    {
        if (!_isLyricViewLoaded || LyricView.Items.Count == 0)
        {
            return false;
        }

        var currentSlice = Data.LyricManager.CurrentLyricSlices.FirstOrDefault(s => s.IsCurrent);
        if (
            currentSlice is null
            || LyricView.ContainerFromItem(currentSlice) is not UIElement container
        )
        {
            return false;
        }

        if (LyricViewer.ActualHeight <= 0 || container.RenderSize.Height <= 0)
        {
            return false;
        }

        _isManualScrolling = false;
        var topInViewport = container
            .TransformToVisual(LyricViewer)
            .TransformPoint(new Point(0, 0))
            .Y;
        var targetOffset =
            LyricViewer.VerticalOffset + topInViewport - LyricViewer.ActualHeight / 2 + 80;

        if (double.IsNaN(targetOffset) || double.IsInfinity(targetOffset))
        {
            return false;
        }

        targetOffset = Math.Max(0, targetOffset);

        MarkProgrammaticScroll(targetOffset);
        LyricViewer.ChangeView(null, targetOffset, null, false);

        return true;
    }

    private void MarkProgrammaticScroll(double targetOffset)
    {
        _isProgrammaticScroll = true;
        _programmaticTargetOffset = targetOffset;

        _programmaticScrollFallbackTimer ??= DispatcherQueue.CreateTimer();
        _programmaticScrollFallbackTimer.Stop();
        _programmaticScrollFallbackTimer.Interval = TimeSpan.FromMilliseconds(900);
        _programmaticScrollFallbackTimer.Tick -= ProgrammaticScrollFallbackTimerTick;
        _programmaticScrollFallbackTimer.Tick += ProgrammaticScrollFallbackTimerTick;
        _programmaticScrollFallbackTimer.Start();
    }

    private void ProgrammaticScrollFallbackTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= ProgrammaticScrollFallbackTimerTick;
        _isProgrammaticScroll = false;
        _programmaticTargetOffset = null;
    }

    private void ClearProgrammaticScrollState()
    {
        _isProgrammaticScroll = false;
        _programmaticTargetOffset = null;
        if (_programmaticScrollFallbackTimer is not null)
        {
            _programmaticScrollFallbackTimer.Stop();
            _programmaticScrollFallbackTimer.Tick -= ProgrammaticScrollFallbackTimerTick;
        }
    }

    public void Dispose()
    {
        _autoScrollDelayTimer.Dispose();
        _titleBarHideTimer?.Dispose();
        _coverLoadWaitCts?.Cancel();
        _coverLoadWaitCts?.Dispose();
        _coverLoadWaitCts = null;
        _ensureLyricScrollCts?.Cancel();
        _ensureLyricScrollCts?.Dispose();
        _ensureLyricScrollCts = null;
        _contentGridMarginAnimationTimer?.Stop();
        _contentGridMarginAnimationTimer?.Tick -= ContentGridMarginAnimationTick;
        _contentGridMarginAnimationTimer = null;
        _programmaticScrollFallbackTimer?.Stop();
        _programmaticScrollFallbackTimer?.Tick -= ProgrammaticScrollFallbackTimerTick;
        _programmaticScrollFallbackTimer = null;
        _lyricAdjustBorderVisual?.StopAnimation("Translation.X");
        _lyricAdjustBorderVisual?.StopAnimation("Opacity");
        _lyricAdjustBorderVisual = null;

        Data.PlayState.PropertyChanged -= OnStateChanged;
        Data.RootPlayBarViewModel?.PropertyChanged -= OnRootPlayBarChanged;
        Data.LyricPage = null;
    }
}
