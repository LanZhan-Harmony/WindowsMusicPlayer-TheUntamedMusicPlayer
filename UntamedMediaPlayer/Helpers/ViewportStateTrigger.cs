using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace UntamedMediaPlayer.Helpers;

internal sealed partial class ViewportStateTrigger : StateTriggerBase, IDisposable
{
    // 宽度断点
    private const double NarrowWidthThreshold = 641;
    private const double LargeWidthThreshold = 850;
    private const double ExtraLargeWidthThreshold = 1000;

    // 高度断点
    private const double NormalHeightThreshold = 500;

    private readonly MainWindow _window = App.MainWindow;
    private InteractionMode _currentInteractionMode = InteractionMode.Mouse;

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    private static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
        nameof(IsEnabled),
        typeof(bool),
        typeof(ViewportStateTrigger),
        new PropertyMetadata(true, OnTriggerPropertyChanged)
    );

    public string State
    {
        get => (string)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State),
        typeof(string),
        typeof(ViewportStateTrigger),
        new PropertyMetadata(null, OnTriggerPropertyChanged)
    );

    public InteractionMode PrimaryInteractionMode
    {
        get => (InteractionMode)GetValue(PrimaryInteractionModeProperty);
        set => SetValue(PrimaryInteractionModeProperty, value);
    }

    private static readonly DependencyProperty PrimaryInteractionModeProperty =
        DependencyProperty.Register(
            nameof(PrimaryInteractionMode),
            typeof(InteractionMode),
            typeof(ViewportStateTrigger),
            new PropertyMetadata(InteractionMode.Undefined, OnTriggerPropertyChanged)
        );

    public ViewportStateTrigger()
    {
        _window.SizeChanged += OnWindowSizeChanged;
        _window.MainContentView.PointerPressed += OnWindowPointerPressed;
    }

    private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        UpdateState();
    }

    private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        InteractionMode newMode = e.Pointer.PointerDeviceType switch
        {
            PointerDeviceType.Mouse => InteractionMode.Mouse,
            PointerDeviceType.Touch => InteractionMode.Touch,
            _ => InteractionMode.Undefined,
        };

        if (newMode != _currentInteractionMode)
        {
            _currentInteractionMode = newMode;
            UpdateState();
        }
    }

    private static void OnTriggerPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ((ViewportStateTrigger)d).UpdateState();
    }

    private void UpdateState()
    {
        if (!IsEnabled)
        {
            SetActive(false);
            return;
        }

        double currentWidth = _window.Width;
        double currentHeight = _window.Height;

        string[] expectedStates =
            State?.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            ) ?? [];

        bool stateMatches = false;
        foreach (var expected in expectedStates)
        {
            if (MatchesState(expected, currentWidth, currentHeight))
            {
                stateMatches = true;
                break;
            }
        }

        bool interactionMatches =
            PrimaryInteractionMode == InteractionMode.Undefined
            || _currentInteractionMode == PrimaryInteractionMode;

        SetActive(stateMatches && interactionMatches);
    }

    private bool MatchesState(string expected, double width, double height)
    {
        return expected switch
        {
            "Narrow" => GetWidthState(width) == ViewportState.Narrow,
            "Normal" => GetWidthState(width) == ViewportState.Normal,
            "Large" => GetWidthState(width) == ViewportState.Large,
            "ExtraLarge" => GetWidthState(width) == ViewportState.ExtraLarge,

            "NormalHeight" => GetHeightState(height) == ViewportState.NormalHeight,
            "NarrowHeight" => GetHeightState(height) == ViewportState.NarrowHeight,

            "ExtraLargeWidthNormalHeight" => GetWidthState(width) == ViewportState.ExtraLarge
                && GetHeightState(height) == ViewportState.NormalHeight,

            "FullScreen" => IsFullScreen(),

            "AnyCompactOverlay" => IsCompactOverlay(),

            "NarrowCompactOverlay" => GetWidthState(width) == ViewportState.Narrow
                && IsCompactOverlay(),

            "NormalWidthNormalHeightCompactOverlay" => GetWidthState(width) == ViewportState.Normal
                && GetHeightState(height) == ViewportState.NormalHeight
                && IsCompactOverlay(),

            "NormalWidthNarrowHeightCompactOverlay" => GetWidthState(width) == ViewportState.Normal
                && GetHeightState(height) == ViewportState.NarrowHeight
                && IsCompactOverlay(),

            _ => false,
        };
    }

    private static ViewportState GetWidthState(double width)
    {
        if (width >= ExtraLargeWidthThreshold)
        {
            return ViewportState.ExtraLarge;
        }
        if (width >= LargeWidthThreshold)
        {
            return ViewportState.Large;
        }
        if (width >= NarrowWidthThreshold)
        {
            return ViewportState.Normal;
        }
        return ViewportState.Narrow;
    }

    private static ViewportState GetHeightState(double height)
    {
        return height >= NormalHeightThreshold
            ? ViewportState.NormalHeight
            : ViewportState.NarrowHeight;
    }

    private bool IsCompactOverlay()
    {
        return _window.AppWindow.Presenter.Kind == AppWindowPresenterKind.CompactOverlay;
    }

    private bool IsFullScreen()
    {
        return _window.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
    }

    public void Dispose()
    {
        _window.SizeChanged -= OnWindowSizeChanged;
        _window.MainContentView.PointerPressed -= OnWindowPointerPressed;
    }
}

internal enum ViewportState
{
    Narrow,
    Normal,
    Large,
    ExtraLarge,
    FullScreen,
    NormalHeight,
    NarrowHeight,
    ExtraLargeWidthNormalHeight,
    AnyCompactOverlay,
    NarrowCompactOverlay,
    NormalWidthNormalHeightCompactOverlay,
    NormalWidthNarrowHeightCompactOverlay,
}

internal enum InteractionMode
{
    Undefined,
    Mouse,
    Touch,
}
