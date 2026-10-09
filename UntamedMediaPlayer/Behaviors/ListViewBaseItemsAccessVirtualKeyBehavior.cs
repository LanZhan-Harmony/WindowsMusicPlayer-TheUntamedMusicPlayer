using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Assigns a stable access-key sequence to the realized item containers.
/// </summary>
internal sealed class ListViewBaseItemsAccessVirtualKeyBehavior : Behavior<ListViewBase>
{
    public string AccessVirtualKeyPrefix
    {
        get => (string)GetValue(AccessVirtualKeyPrefixProperty);
        set => SetValue(AccessVirtualKeyPrefixProperty, value);
    }

    private static readonly DependencyProperty AccessVirtualKeyPrefixProperty =
        DependencyProperty.Register(
            nameof(AccessVirtualKeyPrefix),
            typeof(string),
            typeof(ListViewBaseItemsAccessVirtualKeyBehavior),
            new PropertyMetadata(string.Empty, OnPrefixChanged)
        );

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ContainerContentChanging += OnContainerContentChanging;
        AssociatedObject.Loaded += OnLoaded;
        UpdateRealizedContainers();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ContainerContentChanging -= OnContainerContentChanging;
        AssociatedObject.Loaded -= OnLoaded;
        base.OnDetaching();
    }

    private static void OnPrefixChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is ListViewBaseItemsAccessVirtualKeyBehavior behavior)
        {
            behavior.UpdateRealizedContainers();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        UpdateRealizedContainers();
    }

    private void OnContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args
    )
    {
        if (args.Phase == 0 && !args.InRecycleQueue)
        {
            SetAccessKey(args.ItemContainer, args.ItemIndex);
        }
    }

    private void UpdateRealizedContainers()
    {
        if (AssociatedObject is not { } list)
        {
            return;
        }

        for (int index = 0; index < list.Items.Count; index++)
        {
            if (list.ContainerFromIndex(index) is SelectorItem container)
            {
                SetAccessKey(container, index);
            }
        }
    }

    private void SetAccessKey(DependencyObject container, int index)
    {
        if (container is not Control control)
        {
            return;
        }

        string accessKey = string.Concat(AccessVirtualKeyPrefix, (index + 1).ToString());
        control.AccessKey = accessKey;
    }
}
