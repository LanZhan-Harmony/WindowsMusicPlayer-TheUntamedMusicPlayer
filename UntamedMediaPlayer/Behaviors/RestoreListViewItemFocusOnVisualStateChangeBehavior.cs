using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>Returns focus to an item after its container is recycled during a layout change.</summary>
public sealed class RestoreListViewItemFocusOnVisualStateChangeBehavior : Behavior<ListViewBase>
{
    private object? _focusedItem;
    private bool _restorePending;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        AssociatedObject.GotFocus += OnGotFocus;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        AssociatedObject.GotFocus -= OnGotFocus;
        base.OnDetaching();
    }

    private void OnGotFocus(object sender, RoutedEventArgs args)
    {
        if (args.OriginalSource is DependencyObject source && FindItemContainer(source) is { } container)
        {
            int index = AssociatedObject.IndexFromContainer(container);
            if (index >= 0 && index < AssociatedObject.Items.Count)
            {
                _focusedItem = AssociatedObject.Items[index];
                _restorePending = false;
            }
        }
    }

    private static SelectorItem? FindItemContainer(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current is not null && current is not SelectorItem)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        return current as SelectorItem;
    }

    private void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (args.InRecycleQueue)
        {
            if (args.ItemContainer.FocusState != FocusState.Unfocused)
            {
                int index = sender.IndexFromContainer(args.ItemContainer);
                if (index >= 0 && index < sender.Items.Count)
                {
                    _focusedItem = sender.Items[index];
                    _restorePending = true;
                }
            }

            return;
        }

        if (
            args.Phase == 0
            && _restorePending
            && _focusedItem is not null
            && ReferenceEquals(args.Item, _focusedItem)
        )
        {
            args.ItemContainer.DispatcherQueue.TryEnqueue(() =>
            {
                args.ItemContainer.Focus(FocusState.Programmatic);
                _restorePending = false;
            });
        }
    }
}
