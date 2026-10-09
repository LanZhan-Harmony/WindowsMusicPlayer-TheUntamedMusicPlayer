using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Supplies an automation name for realized grouped-list items that have no text name.
/// </summary>
internal sealed class ListViewBaseGroupHeaderAutomationNameBehavior : Behavior<ListViewBase>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        base.OnDetaching();
    }

    private static void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (
            args.Phase != 0
            || args.InRecycleQueue
            || args.ItemContainer is not DependencyObject container
        )
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(container)))
        {
            string? itemName = args.Item?.ToString();
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                AutomationProperties.SetName(container, itemName);
            }
        }
    }
}
