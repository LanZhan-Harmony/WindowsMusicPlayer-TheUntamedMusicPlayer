using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Contracts.Services;

public interface INavigationService
{
    string NavigationSourcePage { get; }

    void InitializeShell(
        ShellPage shellPage,
        Frame frame,
        NavigationView navigationView,
        Action<string> setNavigationSourcePage
    );

    void InitializeHome(Frame frame);

    bool NavigateShell(
        string destPage,
        object? parameter = null,
        NavigationTransition transition = NavigationTransition.Default
    );

    bool NavigateHome(
        HomeNavigationPage page,
        object? parameter = null,
        HomeNavigationDirection direction = HomeNavigationDirection.Forward
    );

    bool GoBackShell();

    Frame? GetShellFrame();

    NavigationView? GetShellNavigationView();

    ShellPage? GetShellPage();
}
