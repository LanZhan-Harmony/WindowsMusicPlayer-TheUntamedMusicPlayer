using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UntamedMediaPlayer.Controls;

public sealed partial class DisplayModeSelector : UserControl
{
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
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

    private static void OnHeaderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is DisplayModeSelector selector)
        {
            selector.ApplyHeader();
        }
    }

    private void ApplyHeader() => rootNavigationView.Header = Header;
}
