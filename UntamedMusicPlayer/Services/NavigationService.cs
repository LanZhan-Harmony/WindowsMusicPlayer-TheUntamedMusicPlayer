using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using UntamedMusicPlayer.Contracts.Services;
using UntamedMusicPlayer.Models;
using UntamedMusicPlayer.Views;

namespace UntamedMusicPlayer.Services;

public sealed class NavigationService : INavigationService
{
    private ShellPage? _shellPage;
    private Frame? _shellFrame;
    private NavigationView? _shellNavigationView;
    private Frame? _homeFrame;
    private Action<string>? _setNavigationSourcePage;

    public string NavigationSourcePage { get; private set; } = "";

    public void InitializeShell(
        ShellPage shellPage,
        Frame frame,
        NavigationView navigationView,
        Action<string> setNavigationSourcePage
    )
    {
        _shellPage = shellPage;
        _shellFrame = frame;
        _shellNavigationView = navigationView;
        _setNavigationSourcePage = setNavigationSourcePage;
    }

    public void InitializeHome(Frame frame)
    {
        _homeFrame = frame;
    }

    public bool NavigateShell(
        string destPage,
        object? parameter = null,
        NavigationTransition transition = NavigationTransition.Default
    )
    {
        if (_shellFrame is null)
        {
            return false;
        }

        NavigationSourcePage = GetNavigationSourcePage(parameter);
        _setNavigationSourcePage?.Invoke(NavigationSourcePage);
        var infoOverride = transition == NavigationTransition.Suppress
            ? new SuppressNavigationTransitionInfo()
            : null;
        return _shellFrame.Navigate(
            ResolveShellPageType(destPage),
            UnwrapNavigationParameter(parameter),
            infoOverride
        );
    }

    public bool NavigateHome(
        HomeNavigationPage page,
        object? parameter = null,
        HomeNavigationDirection direction = HomeNavigationDirection.Forward
    )
    {
        if (_homeFrame is null)
        {
            return false;
        }

        var infoOverride = new SlideNavigationTransitionInfo
        {
            Effect =
                direction == HomeNavigationDirection.Forward
                    ? SlideNavigationTransitionEffect.FromRight
                    : SlideNavigationTransitionEffect.FromLeft,
        };
        return _homeFrame.Navigate(ResolveHomePageType(page), parameter, infoOverride);
    }

    public bool GoBackShell()
    {
        if (_shellFrame?.CanGoBack != true)
        {
            return false;
        }

        _shellFrame.GoBack();
        return true;
    }

    public Frame? GetShellFrame() => _shellFrame;

    public NavigationView? GetShellNavigationView() => _shellNavigationView;

    public ShellPage? GetShellPage() => _shellPage;

    private static string GetNavigationSourcePage(object? parameter) =>
        parameter switch
        {
            string s => s,
            PlaylistNavigationArgs navArgs => navArgs.FromPage,
            LocalAlbumNavigationArgs navArgs => navArgs.FromPage,
            LocalArtistNavigationArgs navArgs => navArgs.FromPage,
            OnlineAlbumNavigationArgs navArgs => navArgs.FromPage,
            OnlineArtistNavigationArgs navArgs => navArgs.FromPage,
            OnlinePlaylistNavigationArgs navArgs => navArgs.FromPage,
            _ => "",
        };

    private static object? UnwrapNavigationParameter(object? parameter) =>
        parameter switch
        {
            PlaylistNavigationArgs navArgs => navArgs.Playlist,
            LocalAlbumNavigationArgs navArgs => navArgs.Album,
            LocalArtistNavigationArgs navArgs => navArgs.Artist,
            OnlineAlbumNavigationArgs navArgs => navArgs.Album,
            OnlineArtistNavigationArgs navArgs => navArgs.Artist,
            OnlinePlaylistNavigationArgs navArgs => navArgs.Playlist,
            _ => parameter,
        };

    private static Type ResolveShellPageType(string destPage) =>
        destPage switch
        {
            nameof(HomePage) => typeof(HomePage),
            nameof(MusicLibraryPage) => typeof(MusicLibraryPage),
            nameof(PlayQueuePage) => typeof(PlayQueuePage),
            nameof(PlayListsPage) => typeof(PlayListsPage),
            nameof(SettingsPage) => typeof(SettingsPage),
            nameof(LocalAlbumDetailPage) => typeof(LocalAlbumDetailPage),
            nameof(LocalArtistDetailPage) => typeof(LocalArtistDetailPage),
            nameof(PlayListDetailPage) => typeof(PlayListDetailPage),
            nameof(OnlineAlbumDetailPage) => typeof(OnlineAlbumDetailPage),
            nameof(OnlineArtistDetailPage) => typeof(OnlineArtistDetailPage),
            nameof(OnlinePlayListDetailPage) => typeof(OnlinePlayListDetailPage),
            _ => typeof(HomePage),
        };

    private static Type ResolveHomePageType(HomeNavigationPage page) =>
        page switch
        {
            HomeNavigationPage.OnlineSongs => typeof(OnlineSongsPage),
            HomeNavigationPage.OnlineAlbums => typeof(OnlineAlbumsPage),
            HomeNavigationPage.OnlineArtists => typeof(OnlineArtistsPage),
            HomeNavigationPage.OnlinePlayLists => typeof(OnlinePlayListsPage),
            _ => typeof(OnlineSongsPage),
        };
}
