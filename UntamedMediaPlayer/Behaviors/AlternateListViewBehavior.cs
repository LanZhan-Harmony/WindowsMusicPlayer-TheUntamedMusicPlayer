using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;
using UntamedMediaPlayer.Controls.CustomItemPresenters;
using Windows.Foundation.Collections;

namespace UntamedMediaPlayer.Behaviors;

internal sealed class AlternateListViewBehavior : Behavior<ListViewBase>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ActualThemeChanged += OnActualThemeChanged;
        AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        if (AssociatedObject.Items is null)
        {
            return;
        }

        AssociatedObject.Items.VectorChanged += ItemsOnVectorChanged;
        if (AssociatedObject.Items.Count > 0)
        {
            // Update alternate layout on attached if there are items.
            // Item containers may be cached if the list is previously loaded
            // and ContainerContentChanging event is not triggered.
            for (int i = 0; i < AssociatedObject.Items.Count; i++)
            {
                if (AssociatedObject.ContainerFromIndex(i) is SelectorItem itemContainer)
                {
                    UpdateAlternateLayout(itemContainer, i);
                }
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.ActualThemeChanged -= OnActualThemeChanged;
        AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        AssociatedObject.Items?.VectorChanged -= ItemsOnVectorChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (AssociatedObject.Items is null)
        {
            return;
        }

        for (var i = 0; i < AssociatedObject.Items.Count; i++)
        {
            if (AssociatedObject.ContainerFromIndex(i) is SelectorItem itemContainer)
            {
                UpdateAlternateLayout(itemContainer, i);
            }
        }
    }

    private void ItemsOnVectorChanged(
        IObservableVector<object> sender,
        IVectorChangedEventArgs args
    )
    {
        // If the index is at the end we can ignore
        if (args.Index == sender.Count - 1)
        {
            return;
        }

        // Only need to handle Inserted and Removed because we'll handle everything else in the
        // OnContainerContentChanging method
        if (args.CollectionChange is CollectionChange.ItemInserted or CollectionChange.ItemRemoved)
        {
            for (int i = (int)args.Index; i < sender.Count; i++)
            {
                if (AssociatedObject.ContainerFromIndex(i) is SelectorItem itemContainer)
                {
                    UpdateAlternateLayout(itemContainer, i);
                }
            }
        }
    }

    private void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (args.Phase > 0 || args.InRecycleQueue)
        {
            return;
        }

        UpdateAlternateLayout(args.ItemContainer, args.ItemIndex);
    }

    private static void UpdateAlternateLayout(SelectorItem itemContainer, int itemIndex)
    {
        if (itemContainer.FindDescendant<CustomListViewItemPresenter>() is not { } presenter)
        {
            return;
        }

        presenter.IsAlternate = itemIndex % 2 == 0;
    }
}
