using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>Moves keyboard focus to the associated element when it is loaded.</summary>
public sealed class AutoFocusBehavior : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        base.OnDetaching();
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is Control control)
        {
            control.Focus(FocusState.Programmatic);
        }
        else if (sender is UIElement element)
        {
            element.Focus(FocusState.Programmatic);
        }
    }
}
