using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Keeps selection valid when a list switches between item sources.
/// </summary>
internal sealed class ListViewBaseMultipleSourcesBehavior : Behavior<ListViewBase>
{
    private long _itemsSourceChangedToken;

    protected override void OnAttached()
    {
        base.OnAttached();
        _itemsSourceChangedToken = AssociatedObject.RegisterPropertyChangedCallback(
            ItemsControl.ItemsSourceProperty,
            OnItemsSourceChanged
        );
        ReconcileSelection();
    }

    protected override void OnDetaching()
    {
        if (_itemsSourceChangedToken != 0)
        {
            AssociatedObject.UnregisterPropertyChangedCallback(
                ItemsControl.ItemsSourceProperty,
                _itemsSourceChangedToken
            );
        }

        base.OnDetaching();
    }

    private void OnItemsSourceChanged(DependencyObject sender, DependencyProperty property)
    {
        AssociatedObject.DispatcherQueue.TryEnqueue(ReconcileSelection);
    }

    private void ReconcileSelection()
    {
        if (AssociatedObject.SelectionMode == ListViewSelectionMode.None)
        {
            return;
        }

        HashSet<object> currentItems = [.. AssociatedObject.Items];
        if (AssociatedObject.SelectionMode == ListViewSelectionMode.Single)
        {
            if (
                AssociatedObject.SelectedItem is { } selectedItem
                && !currentItems.Contains(selectedItem)
            )
            {
                AssociatedObject.SelectedItem = null;
            }

            return;
        }

        foreach (object selectedItem in AssociatedObject.SelectedItems.ToArray())
        {
            if (!currentItems.Contains(selectedItem))
            {
                AssociatedObject.SelectedItems.Remove(selectedItem);
            }
        }
    }
}
