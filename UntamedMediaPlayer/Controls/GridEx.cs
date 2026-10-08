using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMediaPlayer.Compatibility;

namespace UntamedMediaPlayer.Controls;

/// <summary>Grid with the drop configuration property used by the recovered page markup.</summary>
public sealed partial class GridEx : Grid
{
    public DropConfiguration? DropConfiguration
    {
        get => (DropConfiguration?)GetValue(DropConfigurationProperty);
        set => SetValue(DropConfigurationProperty, value);
    }

    public static readonly DependencyProperty DropConfigurationProperty = DependencyProperty.Register(
        nameof(DropConfiguration),
        typeof(DropConfiguration),
        typeof(GridEx),
        new PropertyMetadata(null, OnDropConfigurationChanged)
    );

    private static void OnDropConfigurationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is GridEx grid)
        {
            UIElementEx.SetDropConfiguration(grid, args.NewValue as DropConfiguration);
        }
    }
}
