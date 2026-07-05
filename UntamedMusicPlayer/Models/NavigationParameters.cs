using UntamedMusicPlayer.Contracts.Models;

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

public sealed record PlaylistNavigationArgs(PlaylistInfo Playlist, string FromPage);

public sealed record LocalAlbumNavigationArgs(LocalAlbumInfo Album, string FromPage);

public sealed record LocalArtistNavigationArgs(LocalArtistInfo Artist, string FromPage);

public sealed record OnlineAlbumNavigationArgs(IBriefOnlineAlbumInfo Album, string FromPage);

public sealed record OnlineArtistNavigationArgs(IBriefOnlineArtistInfo Artist, string FromPage);

public sealed record OnlinePlaylistNavigationArgs(
    IBriefOnlinePlaylistInfo Playlist,
    string FromPage
);
