using System.Windows.Input;

namespace UntamedMediaPlayer.Helpers;

internal sealed partial class PlaylistsDropCommandAdapter : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        // The migrated project has no playlist service yet. Keeping this as a
        // no-op command preserves XAML activation and gives callers a valid
        // ICommand until the service is connected.
    }
}
