using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UntamedMediaPlayer.Services;




namespace UntamedMediaPlayer.Controls;

internal sealed partial class AppChrome : UserControl
{
    private readonly GlobalKeyboardAcceleratorService _keyboardAcceleratorService;

    public AppChrome()
    {
        InitializeComponent();
        _keyboardAcceleratorService = (GlobalKeyboardAcceleratorService)
            Resources["globalKeyboardAccelerators"];
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        _keyboardAcceleratorService.Attach(this);
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _keyboardAcceleratorService.Detach();
    }
}
