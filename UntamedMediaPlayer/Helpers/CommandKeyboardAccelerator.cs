using System.Windows.Input;
using Microsoft.UI.Xaml;
using Windows.System;

namespace UntamedMediaPlayer.Helpers;

/// <summary>
/// Data definition used by GlobalKeyboardAcceleratorService.
/// </summary>
internal sealed class CommandKeyboardAccelerator : DependencyObject
{
    public VirtualKey Key
    {
        get => (VirtualKey)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    private static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        nameof(Key),
        typeof(VirtualKey),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(VirtualKey.None)
    );

    public int KeyCode
    {
        get => (int)GetValue(KeyCodeProperty);
        set => SetValue(KeyCodeProperty, value);
    }

    private static readonly DependencyProperty KeyCodeProperty = DependencyProperty.Register(
        nameof(KeyCode),
        typeof(int),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(0)
    );

    public VirtualKeyModifiers Modifiers
    {
        get => (VirtualKeyModifiers)GetValue(ModifiersProperty);
        set => SetValue(ModifiersProperty, value);
    }

    private static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register(
        nameof(Modifiers),
        typeof(VirtualKeyModifiers),
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(VirtualKeyModifiers.None)
    );

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(CommandKeyboardAccelerator),
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
        typeof(CommandKeyboardAccelerator),
        new PropertyMetadata(null)
    );
}
