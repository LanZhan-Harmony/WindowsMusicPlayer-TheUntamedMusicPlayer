using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

internal class LiveAccessKeyBehavior : Behavior<UIElement>
{
    public string AccessVirtualKey
    {
        get => (string)GetValue(AccessVirtualKeyProperty);
        set => SetValue(AccessVirtualKeyProperty, value);
    }

    public static readonly DependencyProperty AccessVirtualKeyProperty =
        DependencyProperty.Register(
            nameof(AccessVirtualKey),
            typeof(string),
            typeof(LiveAccessKeyBehavior),
            new PropertyMetadata(default(string), OnAccessVirtualKeyChanged)
        );

    protected override void OnAttached()
    {
        base.OnAttached();
        ApplyAccessKey();
    }

    private static void OnAccessVirtualKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LiveAccessKeyBehavior behavior)
        {
            behavior.ApplyAccessKey();
        }
    }

    private void ApplyAccessKey()
    {
        if (AssociatedObject is Control control && !string.IsNullOrEmpty(AccessVirtualKey))
        {
            control.AccessKey = AccessVirtualKey;
        }
    }
}
