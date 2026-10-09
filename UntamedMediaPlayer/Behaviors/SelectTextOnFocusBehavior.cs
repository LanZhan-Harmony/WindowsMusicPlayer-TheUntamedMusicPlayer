using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Selects all text in a TextBox when it receives keyboard focus.
/// </summary>
internal sealed class SelectTextOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.GotFocus += OnGotFocus;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.GotFocus -= OnGotFocus;
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }
}
