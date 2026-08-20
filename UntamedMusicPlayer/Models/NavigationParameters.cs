using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;

namespace UntamedMusicPlayer.Models;

public enum HomeNavigationPage
{
    OnlineSongs,
    OnlineAlbums,
    OnlineArtists,
    OnlinePlayLists,
}

public enum HomeNavigationDirection
{
    Backward,
    Forward,
}

public enum NavigationTransition
{
    Default,
    Suppress,
}

public enum AppTheme
{
    Default,
    Light,
    Dark,
}

public sealed record PlaylistNavigationArgs(PlaylistInfo Playlist, string FromPage);

public sealed record LocalAlbumNavigationArgs(LocalAlbumInfo Album, string FromPage);

public sealed record LocalArtistNavigationArgs(LocalArtistInfo Artist, string FromPage);

public sealed record OnlineAlbumNavigationArgs(IBriefOnlineAlbumInfo Album, string FromPage);

public sealed record OnlineArtistNavigationArgs(IBriefOnlineArtistInfo Artist, string FromPage);

public sealed record OnlinePlaylistNavigationArgs(
    IBriefOnlinePlaylistInfo Playlist,
    string FromPage
);
