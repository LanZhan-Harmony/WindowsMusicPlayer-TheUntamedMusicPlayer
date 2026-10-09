using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

internal sealed partial class SortAndFilterControl : AppBarButton
{
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem),
        typeof(object),
        typeof(SortAndFilterControl),
        new PropertyMetadata(null)
    );

    public SortAndFilterControl()
    {
        DefaultStyleKey = typeof(SortAndFilterControl);
    }
}
