using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using UntamedMediaPlayer.Core.ViewModels;
using UntamedMediaPlayer.Helpers;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class CustomMediaTransportControls : MediaTransportControls
{
    public DropConfiguration? DropConfiguration
    {
        get => (DropConfiguration?)GetValue(DropConfigurationProperty);
        set => SetValue(DropConfigurationProperty, value);
    }

    private static readonly DependencyProperty DropConfigurationProperty =
        DependencyProperty.Register(
            nameof(DropConfiguration),
            typeof(DropConfiguration),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(null, OnDropConfigurationChanged)
        );

    private static void OnDropConfigurationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is CustomMediaTransportControls controls)
        {
            UIElementEx.SetDropConfiguration(controls, args.NewValue as DropConfiguration);
        }
    }

    public MediaPlayerViewModel? MediaPlayerViewModel
    {
        get => (MediaPlayerViewModel?)GetValue(MediaPlayerViewModelProperty);
        set => SetValue(MediaPlayerViewModelProperty, value);
    }

    private static readonly DependencyProperty MediaPlayerViewModelProperty =
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

    private static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
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

    private static readonly DependencyProperty NotificationsProperty = DependencyProperty.Register(
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

    private static readonly DependencyProperty CommandPanelHeaderProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty FullWindowTitleBarProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty TitleBarMarginProperty = DependencyProperty.Register(
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

    private static readonly DependencyProperty SystemTitleBarHeightProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty IsInPlaybackModeProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty IsInVideoOrPlaybackModeProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty IsLiveStreamProperty = DependencyProperty.Register(
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

    private static readonly DependencyProperty IsTitleBarBackgroundVisibleProperty =
        DependencyProperty.Register(
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

    private static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
        nameof(DisplayMode),
        typeof(object),
        typeof(CustomMediaTransportControls),
        new PropertyMetadata(null)
    );

    public FlyoutBase? VideoEnhancerFlyout
    {
        get => (FlyoutBase?)GetValue(VideoEnhancerFlyoutProperty);
        set => SetValue(VideoEnhancerFlyoutProperty, value);
    }

    private static readonly DependencyProperty VideoEnhancerFlyoutProperty =
        DependencyProperty.Register(
            nameof(VideoEnhancerFlyout),
            typeof(FlyoutBase),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(null)
        );

    public MenuFlyout? NowPlayingContextMenuFlyout
    {
        get => (MenuFlyout?)GetValue(NowPlayingContextMenuFlyoutProperty);
        set => SetValue(NowPlayingContextMenuFlyoutProperty, value);
    }

    private static readonly DependencyProperty NowPlayingContextMenuFlyoutProperty =
        DependencyProperty.Register(
            nameof(NowPlayingContextMenuFlyout),
            typeof(MenuFlyout),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(null)
        );
    public bool IsCompactOverlayButtonVisible
    {
        get => (bool)GetValue(IsCompactOverlayButtonVisibleProperty);
        set => SetValue(IsCompactOverlayButtonVisibleProperty, value);
    }

    private static readonly DependencyProperty IsCompactOverlayButtonVisibleProperty =
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

    private static readonly DependencyProperty IsCompactOverlayEnabledProperty =
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

    private static readonly DependencyProperty IsFullWindowButtonVisibleProperty =
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

    private static readonly DependencyProperty IsFullWindowEnabledProperty =
        DependencyProperty.Register(
            nameof(IsFullWindowEnabled),
            typeof(bool),
            typeof(CustomMediaTransportControls),
            new PropertyMetadata(false)
        );

    public CustomMediaTransportControls()
    {
        DefaultStyleKey = typeof(CustomMediaTransportControls);
    }
}
