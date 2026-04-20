using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace UntamedMusicPlayer.Helpers.Animations;

public interface IAttached
{
    void Attach(FrameworkElement element);
    void Detach(FrameworkElement element);
}

public enum ZoomTriggerMode
{
    Threshold,
    Delta,
}

public sealed class ZoomHelper : DependencyObject, IAttached
{
    public const double DefaultSliderScaleFactor = 0.033d;

    public FrameworkElement? Target { get; private set; }

    public event EventHandler? ZoomInRequested;

    public event EventHandler? ZoomOutRequested;

    public event EventHandler<double>? ZoomRequested;

    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold),
        typeof(double),
        typeof(ZoomHelper),
        new PropertyMetadata(60d)
    );

    public ZoomTriggerMode Mode
    {
        get => (ZoomTriggerMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode),
        typeof(ZoomTriggerMode),
        typeof(ZoomHelper),
        new PropertyMetadata(ZoomTriggerMode.Threshold)
    );

    public double ScaleFactor
    {
        get => (double)GetValue(ScaleFactorProperty);
        set => SetValue(ScaleFactorProperty, value);
    }

    public static readonly DependencyProperty ScaleFactorProperty = DependencyProperty.Register(
        nameof(ScaleFactor),
        typeof(double),
        typeof(ZoomHelper),
        new PropertyMetadata(1d)
    );

    public bool TriggerWhenFocused
    {
        get => (bool)GetValue(TriggerWhenFocusedProperty);
        set => SetValue(TriggerWhenFocusedProperty, value);
    }

    public static readonly DependencyProperty TriggerWhenFocusedProperty =
        DependencyProperty.Register(
            nameof(TriggerWhenFocused),
            typeof(bool),
            typeof(ZoomHelper),
            new PropertyMetadata(default(bool))
        );

    public void Attach(FrameworkElement element)
    {
        // 1. Clear existing target
        Target?.PointerWheelChanged -= Target_PointerWheelChanged;

        // 2. Set new target
        Target = element;

        if (Target is null)
        {
            return;
        }

        // 3. Hook events to target
        Target.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler(Target_PointerWheelChanged),
            true
        );
    }

    public void Detach(FrameworkElement target)
    {
        if (target is not null)
        {
            target.RemoveHandler(
                UIElement.PointerWheelChangedEvent,
                new PointerEventHandler(Target_PointerWheelChanged)
            );
            Target = null;
        }
    }

    private void Target_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        // Check if the Ctrl key is pressed
        var isCtrlPressed = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control);

        if (isCtrlPressed || (TriggerWhenFocused && Target is Control c && c.ContainsFocus()))
        {
            // Get the delta of the scroll wheel
            var pointerPoint = e.GetCurrentPoint(sender as FrameworkElement);
            var delta = pointerPoint.Properties.MouseWheelDelta;

            if (Mode is ZoomTriggerMode.Threshold)
            {
                if (delta > Threshold)
                {
                    ZoomInRequested?.Invoke(this, EventArgs.Empty);
                }
                else if (delta < -Threshold)
                {
                    ZoomOutRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (Mode is ZoomTriggerMode.Delta)
            {
                ZoomRequested?.Invoke(this, delta * ScaleFactor);
            }

            e.Handled = true;
        }
    }
}
