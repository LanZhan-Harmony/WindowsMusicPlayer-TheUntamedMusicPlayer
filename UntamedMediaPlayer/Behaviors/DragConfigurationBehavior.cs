using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Enables item dragging and forwards completed drags to the configured command.
/// </summary>
internal sealed class DragConfigurationBehavior : Behavior<ListViewBase>
{
    private bool _previousCanDragItems;
    private bool _previousAllowDrop;

    public bool CanDragItems
    {
        get => (bool)GetValue(CanDragItemsProperty);
        set => SetValue(CanDragItemsProperty, value);
    }

    private static readonly DependencyProperty CanDragItemsProperty = DependencyProperty.Register(
        nameof(CanDragItems),
        typeof(bool),
        typeof(DragConfigurationBehavior),
        new PropertyMetadata(true, OnCanDragItemsChanged)
    );

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(DragConfigurationBehavior),
        new PropertyMetadata(null)
    );

    protected override void OnAttached()
    {
        base.OnAttached();
        _previousCanDragItems = AssociatedObject.CanDragItems;
        _previousAllowDrop = AssociatedObject.AllowDrop;
        AssociatedObject.CanDragItems = CanDragItems;
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragItemsCompleted += OnDragItemsCompleted;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.DragItemsCompleted -= OnDragItemsCompleted;
        AssociatedObject.CanDragItems = _previousCanDragItems;
        AssociatedObject.AllowDrop = _previousAllowDrop;
    }

    private static void OnCanDragItemsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (
            dependencyObject is DragConfigurationBehavior behavior
            && behavior.AssociatedObject is not null
        )
        {
            behavior.AssociatedObject.CanDragItems = (bool)args.NewValue;
        }
    }

    private void OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (Command?.CanExecute(args) == true)
        {
            Command.Execute(args);
        }
    }
}
