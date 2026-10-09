using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Builds menu entries from the current data context when its flyout opens.
/// </summary>
internal sealed class MenuFlyoutItemsDynamicLoadBehavior : Behavior<AppBarButton>
{
    private MenuFlyout? _flyout;
    private INotifyCollectionChanged? _notifyingSource;

    protected override void OnAttached()
    {
        base.OnAttached();
        _flyout = AssociatedObject.Flyout as MenuFlyout;
        if (_flyout is null)
        {
            _flyout = new MenuFlyout();
            AssociatedObject.Flyout = _flyout;
        }

        _flyout.Opening += OnOpening;
        AssociatedObject.DataContextChanged += OnDataContextChanged;
        WatchSource(AssociatedObject.DataContext as INotifyCollectionChanged);
        RebuildItems();
    }

    protected override void OnDetaching()
    {
        _flyout?.Opening -= OnOpening;
        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        WatchSource(null);
        base.OnDetaching();
    }

    private void OnOpening(object? sender, object args)
    {
        RebuildItems();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        WatchSource(args.NewValue as INotifyCollectionChanged);
        RebuildItems();
    }

    private void WatchSource(INotifyCollectionChanged? source)
    {
        if (ReferenceEquals(_notifyingSource, source))
        {
            return;
        }

        _notifyingSource?.CollectionChanged -= OnSourceCollectionChanged;
        _notifyingSource = source;
        _notifyingSource?.CollectionChanged += OnSourceCollectionChanged;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        RebuildItems();
    }

    private void RebuildItems()
    {
        if (_flyout is null)
        {
            return;
        }

        _flyout.Items.Clear();
        if (AssociatedObject.DataContext is not IEnumerable source || source is string)
        {
            return;
        }

        foreach (object? item in source)
        {
            if (item is MenuFlyoutItemBase menuItem)
            {
                _flyout.Items.Add(menuItem);
                continue;
            }

            MenuFlyoutItem generatedItem = new()
            {
                Text = item?.ToString() ?? string.Empty,
                Tag = item,
                IsEnabled = item is not null,
            };
            generatedItem.Click += OnGeneratedItemClick;
            _flyout.Items.Add(generatedItem);
        }
    }

    private void OnGeneratedItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is MenuFlyoutItem { Tag: ICommand command } && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
