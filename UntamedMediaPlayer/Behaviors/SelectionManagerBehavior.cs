using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;
using Windows.System;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>Adds the standard Select All and Escape-to-clear keyboard actions to a list.</summary>
public sealed class SelectionManagerBehavior : Behavior<ListViewBase>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += OnKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.KeyDown -= OnKeyDown;
        base.OnDetaching();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (AssociatedObject.SelectionMode is not (ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
        {
            if (args.Key == VirtualKey.Escape && AssociatedObject.SelectedItem is not null)
            {
                AssociatedObject.SelectedItem = null;
                args.Handled = true;
            }

            return;
        }

        if (args.Key == VirtualKey.A && IsControlDown())
        {
            AssociatedObject.SelectAll();
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.Escape && AssociatedObject.SelectedItems.Count > 0)
        {
            AssociatedObject.SelectedItems.Clear();
            args.Handled = true;
        }
    }

    private static bool IsControlDown() =>
        Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
}
