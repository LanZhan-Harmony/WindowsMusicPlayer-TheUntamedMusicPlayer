using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using UntamedMediaPlayer.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UntamedMediaPlayer.Controls;

public sealed partial class AppChrome : UserControl
{
    private readonly GlobalKeyboardAcceleratorService _keyboardAcceleratorService;

    public AppChrome()
    {
        InitializeComponent();
        _keyboardAcceleratorService = (GlobalKeyboardAcceleratorService)Resources[
            "globalKeyboardAccelerators"
        ];
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args) =>
        _keyboardAcceleratorService.Attach(this);

    private void OnUnloaded(object sender, RoutedEventArgs args) =>
        _keyboardAcceleratorService.Detach();
}
