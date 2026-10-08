using System.Collections;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

internal sealed class LanguageSelectionBehavior : Behavior<AppBarButton>
{
    public object? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(object),
        typeof(LanguageSelectionBehavior),
        new PropertyMetadata(null, OnViewModelChanged)
    );

    protected override void OnAttached()
    {
        base.OnAttached();
        ApplyViewModel();
    }

    protected override void OnDetaching()
    {
        if (ReferenceEquals(AssociatedObject.Command, ViewModel))
        {
            AssociatedObject.Command = null;
        }

        base.OnDetaching();
    }

    private static void OnViewModelChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is LanguageSelectionBehavior behavior)
        {
            behavior.ApplyViewModel();
        }
    }

    private void ApplyViewModel()
    {
        if (AssociatedObject is not { } button)
        {
            return;
        }

        if (ViewModel is ICommand command)
        {
            button.Command = command;
            return;
        }

        if (ViewModel is MenuFlyout menuFlyout)
        {
            button.Flyout = menuFlyout;
            return;
        }

        if (ViewModel is IEnumerable options && ViewModel is not string)
        {
            var flyout = new MenuFlyout();
            foreach (object? option in options)
            {
                if (option is MenuFlyoutItemBase menuItem)
                {
                    flyout.Items.Add(menuItem);
                    continue;
                }

                var item = new MenuFlyoutItem { Text = option?.ToString() ?? string.Empty, Tag = option };
                item.Click += OnLanguageItemClick;
                flyout.Items.Add(item);
            }

            button.Flyout = flyout;
        }
    }

    private static void OnLanguageItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is MenuFlyoutItem { Tag: ICommand command } && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
