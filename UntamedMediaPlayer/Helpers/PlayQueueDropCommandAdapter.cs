using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace UntamedMediaPlayer.Helpers;

internal sealed partial class PlayQueueDropCommandAdapter : DependencyObject, ICommand
{
    private EventHandler? _canExecuteChanged;

    public bool OpenPlayPage
    {
        get => (bool)GetValue(OpenPlayPageProperty);
        set => SetValue(OpenPlayPageProperty, value);
    }

    private static readonly DependencyProperty OpenPlayPageProperty = DependencyProperty.Register(
        nameof(OpenPlayPage),
        typeof(bool),
        typeof(PlayQueueDropCommandAdapter),
        new PropertyMetadata(false, OnOpenPlayPageChanged)
    );

    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        // Queue operations are supplied by the playback service when it is connected.
    }

    private static void OnOpenPlayPageChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args
    )
    {
        if (dependencyObject is PlayQueueDropCommandAdapter command)
        {
            command._canExecuteChanged?.Invoke(command, EventArgs.Empty);
        }
    }
}
