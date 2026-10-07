using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// 为 ListViewBase 配置拖放。简化实现：启用拖放并在拖放完成时执行 Command。
/// </summary>
internal class DragConfigurationBehavior : Behavior<ListViewBase>
{
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(DragConfigurationBehavior),
            new PropertyMetadata(null)
        );

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.CanDragItems = true;
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragItemsCompleted += OnDragItemsCompleted;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.DragItemsCompleted -= OnDragItemsCompleted;
    }

    private void OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (Command?.CanExecute(args) == true)
        {
            Command.Execute(args);
        }
    }
}
