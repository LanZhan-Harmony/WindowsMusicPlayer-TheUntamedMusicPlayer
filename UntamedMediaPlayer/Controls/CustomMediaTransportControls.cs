using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMediaPlayer.Core.ViewModels;

namespace UntamedMediaPlayer.Controls;

public sealed partial class CustomMediaTransportControls : Control
{
    public MediaPlayerViewModel? MediaPlayerViewModel
    {
        get => (MediaPlayerViewModel?)GetValue(MediaPlayerViewModelProperty);
        set => SetValue(MediaPlayerViewModelProperty, value);
    }

    public static readonly DependencyProperty MediaPlayerViewModelProperty =
        DependencyProperty.Register(
            nameof(MediaPlayerViewModel),
            typeof(MediaPlayerViewModel),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(null)
        );

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public object? Notifications
    {
        get => GetValue(NotificationsProperty);
        set => SetValue(NotificationsProperty, value);
    }

    public static readonly DependencyProperty NotificationsProperty = DependencyProperty.Register(
        nameof(Notifications),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public object? CommandPanelHeader
    {
        get => GetValue(CommandPanelHeaderProperty);
        set => SetValue(CommandPanelHeaderProperty, value);
    }

    public static readonly DependencyProperty CommandPanelHeaderProperty = DependencyProperty.Register(
        nameof(CommandPanelHeader),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public object? FullWindowTitleBar
    {
        get => GetValue(FullWindowTitleBarProperty);
        set => SetValue(FullWindowTitleBarProperty, value);
    }

    public static readonly DependencyProperty FullWindowTitleBarProperty = DependencyProperty.Register(
        nameof(FullWindowTitleBar),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public object? TitleBarMargin
    {
        get => GetValue(TitleBarMarginProperty);
        set => SetValue(TitleBarMarginProperty, value);
    }

    public static readonly DependencyProperty TitleBarMarginProperty = DependencyProperty.Register(
        nameof(TitleBarMargin),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public double SystemTitleBarHeight
    {
        get => (double)GetValue(SystemTitleBarHeightProperty);
        set => SetValue(SystemTitleBarHeightProperty, value);
    }

    public static readonly DependencyProperty SystemTitleBarHeightProperty = DependencyProperty.Register(
        nameof(SystemTitleBarHeight),
        typeof(double),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(0d)
    );

    public bool IsInPlaybackMode
    {
        get => (bool)GetValue(IsInPlaybackModeProperty);
        set => SetValue(IsInPlaybackModeProperty, value);
    }

    public static readonly DependencyProperty IsInPlaybackModeProperty = DependencyProperty.Register(
        nameof(IsInPlaybackMode),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public bool IsInVideoOrPlaybackMode
    {
        get => (bool)GetValue(IsInVideoOrPlaybackModeProperty);
        set => SetValue(IsInVideoOrPlaybackModeProperty, value);
    }

    public static readonly DependencyProperty IsInVideoOrPlaybackModeProperty = DependencyProperty.Register(
        nameof(IsInVideoOrPlaybackMode),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public bool IsLiveStream
    {
        get => (bool)GetValue(IsLiveStreamProperty);
        set => SetValue(IsLiveStreamProperty, value);
    }

    public static readonly DependencyProperty IsLiveStreamProperty = DependencyProperty.Register(
        nameof(IsLiveStream),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public bool IsTitleBarBackgroundVisible
    {
        get => (bool)GetValue(IsTitleBarBackgroundVisibleProperty);
        set => SetValue(IsTitleBarBackgroundVisibleProperty, value);
    }

    public static readonly DependencyProperty IsTitleBarBackgroundVisibleProperty = DependencyProperty.Register(
        nameof(IsTitleBarBackgroundVisible),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public object? DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
        nameof(DisplayMode),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );
    public bool IsCompactOverlayButtonVisible
    {
        get => (bool)GetValue(IsCompactOverlayButtonVisibleProperty);
        set => SetValue(IsCompactOverlayButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsCompactOverlayButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsCompactOverlayButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsCompactOverlayEnabled
    {
        get => (bool)GetValue(IsCompactOverlayEnabledProperty);
        set => SetValue(IsCompactOverlayEnabledProperty, value);
    }

    public static readonly DependencyProperty IsCompactOverlayEnabledProperty =
        DependencyProperty.Register(
            nameof(IsCompactOverlayEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsFullWindowButtonVisible
    {
        get => (bool)GetValue(IsFullWindowButtonVisibleProperty);
        set => SetValue(IsFullWindowButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsFullWindowButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFullWindowButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsFullWindowEnabled
    {
        get => (bool)GetValue(IsFullWindowEnabledProperty);
        set => SetValue(IsFullWindowEnabledProperty, value);
    }

    public static readonly DependencyProperty IsFullWindowEnabledProperty =
        DependencyProperty.Register(
            nameof(IsFullWindowEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsNextTrackButtonVisible
    {
        get => (bool)GetValue(IsNextTrackButtonVisibleProperty);
        set => SetValue(IsNextTrackButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsNextTrackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsNextTrackButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsPlaybackRateButtonVisible
    {
        get => (bool)GetValue(IsPlaybackRateButtonVisibleProperty);
        set => SetValue(IsPlaybackRateButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsPlaybackRateButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsPlaybackRateButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsPlaybackRateEnabled
    {
        get => (bool)GetValue(IsPlaybackRateEnabledProperty);
        set => SetValue(IsPlaybackRateEnabledProperty, value);
    }

    public static readonly DependencyProperty IsPlaybackRateEnabledProperty =
        DependencyProperty.Register(
            nameof(IsPlaybackRateEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsPreviousTrackButtonVisible
    {
        get => (bool)GetValue(IsPreviousTrackButtonVisibleProperty);
        set => SetValue(IsPreviousTrackButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsPreviousTrackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsPreviousTrackButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsRepeatButtonVisible
    {
        get => (bool)GetValue(IsRepeatButtonVisibleProperty);
        set => SetValue(IsRepeatButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsRepeatButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsRepeatButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsRepeatEnabled
    {
        get => (bool)GetValue(IsRepeatEnabledProperty);
        set => SetValue(IsRepeatEnabledProperty, value);
    }

    public static readonly DependencyProperty IsRepeatEnabledProperty = DependencyProperty.Register(
        nameof(IsRepeatEnabled),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public bool IsSkipBackwardButtonVisible
    {
        get => (bool)GetValue(IsSkipBackwardButtonVisibleProperty);
        set => SetValue(IsSkipBackwardButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsSkipBackwardButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsSkipBackwardButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsSkipBackwardEnabled
    {
        get => (bool)GetValue(IsSkipBackwardEnabledProperty);
        set => SetValue(IsSkipBackwardEnabledProperty, value);
    }

    public static readonly DependencyProperty IsSkipBackwardEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSkipBackwardEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsSkipForwardButtonVisible
    {
        get => (bool)GetValue(IsSkipForwardButtonVisibleProperty);
        set => SetValue(IsSkipForwardButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsSkipForwardButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsSkipForwardButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsSkipForwardEnabled
    {
        get => (bool)GetValue(IsSkipForwardEnabledProperty);
        set => SetValue(IsSkipForwardEnabledProperty, value);
    }

    public static readonly DependencyProperty IsSkipForwardEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSkipForwardEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsZoomButtonVisible
    {
        get => (bool)GetValue(IsZoomButtonVisibleProperty);
        set => SetValue(IsZoomButtonVisibleProperty, value);
    }

    public static readonly DependencyProperty IsZoomButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsZoomButtonVisible),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public bool IsZoomEnabled
    {
        get => (bool)GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    public static readonly DependencyProperty IsZoomEnabledProperty = DependencyProperty.Register(
        nameof(IsZoomEnabled),
        typeof(bool),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(false)
    );

    public CustomMediaTransportControls()
    {
        DefaultStyleKey = typeof(CustomMediaTransportControls);
    }
}
