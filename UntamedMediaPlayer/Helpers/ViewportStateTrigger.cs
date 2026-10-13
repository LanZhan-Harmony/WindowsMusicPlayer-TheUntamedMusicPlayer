using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace UntamedMediaPlayer.Helpers;

internal sealed partial class ViewportStateTrigger : StateTriggerBase, IDisposable
{
    // Width breakpoints
    private const double NarrowWidthThreshold = 641;
    private const double LargeWidthThreshold = 850;
    private const double ExtraLargeWidthThreshold = 1000;

    // Height breakpoints
    private const double NormalHeightThreshold = 500;

    private readonly MainWindow _window = App.MainWindow;
    private InteractionMode _currentInteractionMode = InteractionMode.Mouse;

    // State 变化时解析一次，缓存为 flags 数组（每组内部是 AND，组间是 OR）
    private ViewportStateFlags[] _expectedStateGroups = [];

    // 缓存上一次的激活状态，避免重复触发 VisualStateManager
    private bool _lastActive;

    private bool _pointerSubscribed;
    private bool _disposed;

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
        new PropertyMetadata(null, OnStatePropertyChanged)
    );

    public InteractionMode? PrimaryInteractionMode
    {
        get => (InteractionMode?)GetValue(PrimaryInteractionModeProperty);
        set => SetValue(PrimaryInteractionModeProperty, value);
    }

    private static readonly DependencyProperty PrimaryInteractionModeProperty =
        DependencyProperty.Register(
            nameof(PrimaryInteractionMode),
            typeof(InteractionMode?),
            typeof(ViewportStateTrigger),
            new PropertyMetadata(null, OnPrimaryInteractionModeChanged)
        );

    public ViewportStateTrigger()
    {
        _window.SizeChanged += OnWindowSizeChanged;
        _window.AppWindow.Changed += OnAppWindowChanged;
        // 指针事件延迟到 PrimaryInteractionMode 被设置时再订阅
    }

    private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        UpdateState();
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        // 只关心 Presenter 的变化（FullScreen / CompactOverlay 切换）
        if (args.DidPresenterChange)
        {
            UpdateState();
        }
    }

    private void OnWindowPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        InteractionMode newMode = e.Pointer.PointerDeviceType switch
        {
            PointerDeviceType.Mouse => InteractionMode.Mouse,
            PointerDeviceType.Touch => InteractionMode.Touch,
            _ => InteractionMode.Mouse,
        };

        if (newMode != _currentInteractionMode)
        {
            _currentInteractionMode = newMode;
            UpdateState();
        }
    }

    private static void OnStatePropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ViewportStateTrigger trigger = (ViewportStateTrigger)d;
        trigger._expectedStateGroups = ParseStateGroups((string)e.NewValue);
        trigger.UpdateState();
    }

    private static void OnPrimaryInteractionModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ViewportStateTrigger trigger = (ViewportStateTrigger)d;
        trigger.UpdatePointerSubscription();
        trigger.UpdateState();
    }

    private static void OnTriggerPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ((ViewportStateTrigger)d).UpdateState();
    }

    private void UpdatePointerSubscription()
    {
        bool needPointer = PrimaryInteractionMode is not null;

        if (needPointer && !_pointerSubscribed)
        {
            _window.MainContentView.PointerPressed += OnWindowPointerPressed;
            _pointerSubscribed = true;
        }
        else if (!needPointer && _pointerSubscribed)
        {
            _window.MainContentView.PointerPressed -= OnWindowPointerPressed;
            _pointerSubscribed = false;
        }
    }

    private void UpdateState()
    {
        if (_disposed)
        {
            return;
        }

        if (!IsEnabled)
        {
            if (_lastActive)
            {
                SetActive(false);
                _lastActive = false;
            }
            return;
        }

        ViewportStateFlags current = ComputeCurrentFlags();

        bool stateMatches = false;
        Span<ViewportStateFlags> groups = _expectedStateGroups.AsSpan();
        for (int i = 0; i < groups.Length; i++)
        {
            ViewportStateFlags group = groups[i];
            if (group != ViewportStateFlags.None && (current & group) == group)
            {
                stateMatches = true;
                break;
            }
        }

        bool interactionMatches =
            PrimaryInteractionMode is null || _currentInteractionMode == PrimaryInteractionMode;

        bool active = stateMatches && interactionMatches;
        if (active != _lastActive)
        {
            SetActive(active);
            _lastActive = active;
        }
    }

    private ViewportStateFlags ComputeCurrentFlags()
    {
        ViewportStateFlags flags = ViewportStateFlags.None;

        double width = _window.Width;
        double height = _window.Height;

        // Width 分档（互斥，恰好命中一档）
        if (width >= ExtraLargeWidthThreshold)
        {
            flags |= ViewportStateFlags.ExtraLargeWidth;
        }
        else if (width >= LargeWidthThreshold)
        {
            flags |= ViewportStateFlags.LargeWidth;
        }
        else if (width >= NarrowWidthThreshold)
        {
            flags |= ViewportStateFlags.NormalWidth;
        }
        else
        {
            flags |= ViewportStateFlags.NarrowWidth;
        }

        // Height 分档
        flags |=
            height >= NormalHeightThreshold
                ? ViewportStateFlags.NormalHeight
                : ViewportStateFlags.NarrowHeight;

        // Presenter 模式（FullScreen 与 CompactOverlay 互斥）
        AppWindowPresenterKind kind = _window.AppWindow.Presenter.Kind;
        if (kind == AppWindowPresenterKind.FullScreen)
        {
            flags |= ViewportStateFlags.FullScreen;
        }
        else if (kind == AppWindowPresenterKind.CompactOverlay)
        {
            flags |= ViewportStateFlags.CompactOverlay;
        }

        return flags;
    }

    private static ViewportStateFlags[] ParseStateGroups(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return [];
        }

        string[] parts = state.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        ViewportStateFlags[] result = new ViewportStateFlags[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            result[i] = ParseStateGroup(parts[i]);
        }
        return result;
    }

    private static ViewportStateFlags ParseStateGroup(string state) =>
        state switch
        {
            "Narrow" => ViewportStateFlags.NarrowWidth,
            "Normal" => ViewportStateFlags.NormalWidth,
            "Large" => ViewportStateFlags.LargeWidth,
            "ExtraLarge" => ViewportStateFlags.ExtraLargeWidth,

            "NormalHeight" => ViewportStateFlags.NormalHeight,
            "NarrowHeight" => ViewportStateFlags.NarrowHeight,

            "ExtraLargeWidthNormalHeight" => ViewportStateFlags.ExtraLargeWidth
                | ViewportStateFlags.NormalHeight,

            "FullScreen" => ViewportStateFlags.FullScreen,
            "AnyCompactOverlay" => ViewportStateFlags.CompactOverlay,
            "NarrowCompactOverlay" => ViewportStateFlags.NarrowWidth
                | ViewportStateFlags.CompactOverlay,
            "NormalWidthNormalHeightCompactOverlay" => ViewportStateFlags.NormalWidth
                | ViewportStateFlags.NormalHeight
                | ViewportStateFlags.CompactOverlay,
            "NormalWidthNarrowHeightCompactOverlay" => ViewportStateFlags.NormalWidth
                | ViewportStateFlags.NarrowHeight
                | ViewportStateFlags.CompactOverlay,

            _ => ViewportStateFlags.None,
        };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _window.SizeChanged -= OnWindowSizeChanged;
        _window.AppWindow.Changed -= OnAppWindowChanged;

        if (_pointerSubscribed)
        {
            _window.MainContentView.PointerPressed -= OnWindowPointerPressed;
            _pointerSubscribed = false;
        }
    }
}

[Flags]
internal enum ViewportStateFlags
{
    None = 0,

    NarrowWidth = 1 << 0,
    NormalWidth = 1 << 1,
    LargeWidth = 1 << 2,
    ExtraLargeWidth = 1 << 3,

    NarrowHeight = 1 << 4,
    NormalHeight = 1 << 5,

    FullScreen = 1 << 6,
    CompactOverlay = 1 << 7,
}

internal enum InteractionMode
{
    Mouse,
    Touch,
}
