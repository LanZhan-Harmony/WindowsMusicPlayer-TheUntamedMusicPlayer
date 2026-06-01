using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Services;

public sealed class NavigationService : INavigationService
{
    public bool NavigateShell(
        string destPage,
        object? parameter = null,
        NavigationTransitionInfo? infoOverride = null
    )
    {
        if (Data.ShellPage is null)
        {
            return false;
        }

        Data.ShellPage.Navigate(destPage, parameter, infoOverride);
        return true;
    }

    public bool NavigateHome(
        Type page,
        object? parameter = null,
        NavigationTransitionInfo? infoOverride = null
    )
    {
        var frame = Data.HomePage?.GetFrame();
        if (frame is null)
        {
            return false;
        }

        frame.Navigate(page, parameter, infoOverride);
        return true;
    }

    public bool GoBackShell()
    {
        if (Data.ShellPage is null)
        {
            return false;
        }

        Data.ShellPage.GoBack();
        return true;
    }

    public Frame? GetShellFrame() => Data.ShellPage?.GetFrame();

    public NavigationView? GetShellNavigationView() => Data.ShellPage?.GetNavigationView();

    public ShellPage? GetShellPage() => Data.ShellPage;
}