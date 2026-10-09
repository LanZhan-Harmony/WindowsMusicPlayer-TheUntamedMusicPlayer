using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;




namespace UntamedMediaPlayer.Controls;

internal sealed partial class DisplayModeSelector : UserControl
{
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    private static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(object),
        typeof(DisplayModeSelector),
        new PropertyMetadata(null, OnHeaderChanged)
    );

    public DisplayModeSelector()
    {
        InitializeComponent();
        ApplyHeader();
    }

    private static void OnHeaderChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (sender is DisplayModeSelector selector)
        {
            selector.ApplyHeader();
        }
    }

    private void ApplyHeader()
    {
        rootNavigationView.Header = Header;
    }
}
