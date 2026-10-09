using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using UntamedMediaPlayer.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// Enables drag and drop on the responsive repeater and forwards drops to a command.
/// </summary>
internal sealed class ResponsiveItemsRepeaterListDragConfigurationBehavior
    : Behavior<ResponsiveItemsRepeaterList>
{
    private bool _previousAllowDrop;

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(ResponsiveItemsRepeaterListDragConfigurationBehavior),
        new PropertyMetadata(null)
    );

    protected override void OnAttached()
    {
        base.OnAttached();
        _previousAllowDrop = AssociatedObject.AllowDrop;
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.DragOver -= OnDragOver;
        AssociatedObject.Drop -= OnDrop;
        AssociatedObject.AllowDrop = _previousAllowDrop;
        base.OnDetaching();
    }

    private void OnDragOver(object sender, DragEventArgs args)
    {
        if (Command?.CanExecute(args) == true)
        {
            args.AcceptedOperation = DataPackageOperation.Link;
            args.Handled = true;
        }
    }

    private void OnDrop(object sender, DragEventArgs args)
    {
        if (Command?.CanExecute(args) == true)
        {
            Command.Execute(args);
            args.Handled = true;
        }
    }
}
