using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMediaPlayer.Helpers;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// NavigationViewItem with the recovered markup's drop configuration.
/// </summary>
internal sealed partial class NavigationViewItemEx : NavigationViewItem
{
    public DropConfiguration? DropConfiguration
    {
        get => (DropConfiguration?)GetValue(DropConfigurationProperty);
        set => SetValue(DropConfigurationProperty, value);
    }

    private static readonly DependencyProperty DropConfigurationProperty =
        DependencyProperty.Register(
            nameof(DropConfiguration),
            typeof(DropConfiguration),
            typeof(NavigationViewItemEx),
            new PropertyMetadata(null, OnDropConfigurationChanged)
        );

    private static void OnDropConfigurationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is NavigationViewItemEx item)
        {
            UIElementEx.SetDropConfiguration(item, args.NewValue as DropConfiguration);
        }
    }
}
