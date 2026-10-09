using System.Windows.Input;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace UntamedMediaPlayer.Helpers;

internal sealed class DropConfiguration : DependencyObject
{
    public DataPackageOperation AcceptedOperation
    {
        get => (DataPackageOperation)GetValue(AcceptedOperationProperty);
        set => SetValue(AcceptedOperationProperty, value);
    }

    private static readonly DependencyProperty AcceptedOperationProperty =
        DependencyProperty.Register(
            nameof(AcceptedOperation),
            typeof(DataPackageOperation),
            typeof(DropConfiguration),
            new PropertyMetadata(DataPackageOperation.Copy)
        );

    public DragAndDropUIOverride? AcceptedUIOverride
    {
        get => (DragAndDropUIOverride?)GetValue(AcceptedUIOverrideProperty);
        set => SetValue(AcceptedUIOverrideProperty, value);
    }

    private static readonly DependencyProperty AcceptedUIOverrideProperty =
        DependencyProperty.Register(
            nameof(AcceptedUIOverride),
            typeof(DragAndDropUIOverride),
            typeof(DropConfiguration),
            new PropertyMetadata(null)
        );

    public DragAndDropUIOverride? RejectedUIOverride
    {
        get => (DragAndDropUIOverride?)GetValue(RejectedUIOverrideProperty);
        set => SetValue(RejectedUIOverrideProperty, value);
    }

    private static readonly DependencyProperty RejectedUIOverrideProperty =
        DependencyProperty.Register(
            nameof(RejectedUIOverride),
            typeof(DragAndDropUIOverride),
            typeof(DropConfiguration),
            new PropertyMetadata(null)
        );

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(DropConfiguration),
        new PropertyMetadata(null)
    );
}
