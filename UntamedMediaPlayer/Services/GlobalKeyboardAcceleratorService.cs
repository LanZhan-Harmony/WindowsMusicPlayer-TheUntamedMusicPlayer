using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UntamedMediaPlayer.Compatibility;
using Windows.System;

namespace UntamedMediaPlayer.Services;

/// <summary>Registers configured accelerators on a visual root and routes them to commands.</summary>
public sealed class GlobalKeyboardAcceleratorService
{
    private readonly Dictionary<CommandKeyboardAccelerator, KeyboardAccelerator> _registered = new();
    private UIElement? _target;

    public ObservableCollection<CommandKeyboardAccelerator> KeyboardAccelerators { get; } = new();

    public event EventHandler<CommandKeyboardAccelerator>? Invoked;

    public GlobalKeyboardAcceleratorService()
    {
        KeyboardAccelerators.CollectionChanged += OnKeyboardAcceleratorsChanged;
    }

    public void Attach(UIElement target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(_target, target))
        {
            return;
        }

        Detach();
        _target = target;
        RegisterAll();
    }

    public void Detach()
    {
        if (_target is null)
        {
            return;
        }

        foreach (var pair in _registered)
        {
            pair.Value.Invoked -= OnAcceleratorInvoked;
            _target.KeyboardAccelerators.Remove(pair.Value);
        }

        _registered.Clear();
        _target = null;
    }

    private void OnKeyboardAcceleratorsChanged(
        object? sender,
        NotifyCollectionChangedEventArgs args
    ) => RegisterAll();

    private void RegisterAll()
    {
        if (_target is null)
        {
            return;
        }

        DetachRegisteredAccelerators();
        foreach (var definition in KeyboardAccelerators)
        {
            var accelerator = new KeyboardAccelerator
            {
                Key = definition.KeyCode == 0 ? definition.Key : (VirtualKey)definition.KeyCode,
                Modifiers = definition.Modifiers,
            };
            accelerator.Invoked += OnAcceleratorInvoked;
            _target.KeyboardAccelerators.Add(accelerator);
            _registered.Add(definition, accelerator);
        }
    }

    private void DetachRegisteredAccelerators()
    {
        if (_target is null)
        {
            return;
        }

        foreach (var accelerator in _registered.Values)
        {
            accelerator.Invoked -= OnAcceleratorInvoked;
            _target.KeyboardAccelerators.Remove(accelerator);
        }

        _registered.Clear();
    }

    private void OnAcceleratorInvoked(
        KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args
    )
    {
        var definition = _registered.FirstOrDefault(pair => ReferenceEquals(pair.Value, sender)).Key;
        if (definition is null)
        {
            return;
        }

        Invoked?.Invoke(this, definition);
        if (definition.Command?.CanExecute(definition.CommandParameter) == true)
        {
            definition.Command.Execute(definition.CommandParameter);
            args.Handled = true;
        }
    }
}
