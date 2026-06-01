using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Contracts.Services;

public interface INavigationService
{
    bool NavigateShell(
        string destPage,
        object? parameter = null,
        NavigationTransitionInfo? infoOverride = null
    );

    bool NavigateHome(
        Type page,
        object? parameter = null,
        NavigationTransitionInfo? infoOverride = null
    );

    bool GoBackShell();

    Frame? GetShellFrame();

    NavigationView? GetShellNavigationView();

    ShellPage? GetShellPage();
}