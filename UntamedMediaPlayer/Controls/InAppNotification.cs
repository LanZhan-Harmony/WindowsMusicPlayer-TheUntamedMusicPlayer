using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// A lightweight content notification surface for the migrated transport controls.
/// </summary>
internal sealed partial class InAppNotification : ContentControl
{
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    private static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(InAppNotification),
        new PropertyMetadata(false, OnIsOpenChanged)
    );

    public InAppNotification()
    {
        Visibility = Visibility.Collapsed;
    }

    private static void OnIsOpenChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is InAppNotification notification)
        {
            notification.Visibility = (bool)args.NewValue
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
