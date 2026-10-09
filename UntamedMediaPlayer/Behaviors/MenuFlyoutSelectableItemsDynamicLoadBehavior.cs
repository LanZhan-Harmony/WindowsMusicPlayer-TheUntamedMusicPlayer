using System.Collections;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.Xaml.Interactivity;
using UntamedMediaPlayer.Controls;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Creates selectable flyout entries from the converter's current option sequence.
/// </summary>
internal sealed class MenuFlyoutSelectableItemsDynamicLoadBehavior : Behavior<AppBarButton>
{
    private MenuFlyout? _flyout;

    public IValueConverter? Converter
    {
        get => (IValueConverter?)GetValue(ConverterProperty);
        set => SetValue(ConverterProperty, value);
    }

    private static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(
        nameof(Converter),
        typeof(IValueConverter),
        typeof(MenuFlyoutSelectableItemsDynamicLoadBehavior),
        new PropertyMetadata(null)
    );

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
        RebuildItems();
    }

    protected override void OnDetaching()
    {
        _flyout?.Opening -= OnOpening;
        AssociatedObject.DataContextChanged -= OnDataContextChanged;
        base.OnDetaching();
    }

    private void OnOpening(object? sender, object args)
    {
        RebuildItems();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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
        object? source = AssociatedObject.DataContext;
        if (Converter is not null)
        {
            source = Converter.Convert(source, typeof(IEnumerable), AssociatedObject, string.Empty);
        }

        if (source is MenuFlyout menuFlyout)
        {
            foreach (MenuFlyoutItemBase? item in menuFlyout.Items)
            {
                _flyout.Items.Add(item);
            }

            return;
        }

        if (source is not IEnumerable values || source is string)
        {
            return;
        }

        foreach (object? value in values)
        {
            ToggleMenuFlyoutItem item = new()
            {
                Text = value?.ToString() ?? string.Empty,
                Tag = value,
                IsChecked =
                    AssociatedObject is SortAndFilterControl sortControl
                    && Equals(sortControl.SelectedItem, value),
                IsEnabled = value is not null,
            };
            item.Click += OnItemClick;
            _flyout.Items.Add(item);
        }
    }

    private void OnItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is not ToggleMenuFlyoutItem menuItem)
        {
            return;
        }

        if (AssociatedObject is SortAndFilterControl sortControl)
        {
            sortControl.SelectedItem = menuItem.Tag;
        }

        if (menuItem.Tag is ICommand command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
