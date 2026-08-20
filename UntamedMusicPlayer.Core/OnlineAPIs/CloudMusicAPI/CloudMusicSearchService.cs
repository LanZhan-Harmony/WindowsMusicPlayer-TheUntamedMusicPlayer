using UntamedMusicPlayer.Core.Contracts.Models;
using UntamedMusicPlayer.Core.Models;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Helpers;
using UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI.Models;
using UntamedMusicPlayer.OnlineAPI.CloudMusicAPI;

namespace UntamedMusicPlayer.Core.OnlineAPIs.CloudMusicAPI;

public sealed record CloudSearchPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    bool HasMore,
    string Query
);

/// <summary>
/// Owns Cloud Music search pagination and provider-specific result lists.
/// Application services consume page results instead of mutating provider models.
/// </summary>
public sealed class CloudMusicSearchService
{
    private readonly CloudMusicApiService _api;
    private readonly CloudOnlineSongInfoList _songs = new();
    private readonly CloudOnlineAlbumInfoList _albums = new();
    private readonly CloudOnlineArtistInfoList _artists = new();
    private readonly CloudOnlinePlaylistInfoList _playlists = new();

    public CloudMusicSearchService(CloudMusicApiService api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<CloudSearchPage<IBriefOnlineSongInfo>> SearchSongsAsync(string query)
    {
        await CloudSongSearchHelper.SearchSongsAsync(query, _songs, _api);
        return new CloudSearchPage<IBriefOnlineSongInfo>(
            _songs.ToArray(),
            _songs.SongCount,
            !_songs.HasAllLoaded,
            _songs.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlineSongInfo>> SearchMoreSongsAsync()
    {
        var previousCount = _songs.Count;
        await CloudSongSearchHelper.SearchMoreSongsAsync(_songs, _api);
        return new CloudSearchPage<IBriefOnlineSongInfo>(
            _songs.Skip(previousCount).ToArray(),
            _songs.SongCount,
            !_songs.HasAllLoaded && _songs.Count > previousCount,
            _songs.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlineAlbumInfo>> SearchAlbumsAsync(string query)
    {
        await CloudAlbumSearchHelper.SearchAlbumsAsync(query, _albums, _api);
        return new CloudSearchPage<IBriefOnlineAlbumInfo>(
            _albums.ToArray(),
            _albums.AlbumCount,
            !_albums.HasAllLoaded,
            _albums.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlineAlbumInfo>> SearchMoreAlbumsAsync()
    {
        var previousCount = _albums.Count;
        await CloudAlbumSearchHelper.SearchMoreAlbumsAsync(_albums, _api);
        return new CloudSearchPage<IBriefOnlineAlbumInfo>(
            _albums.Skip(previousCount).ToArray(),
            _albums.AlbumCount,
            !_albums.HasAllLoaded && _albums.Count > previousCount,
            _albums.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlineArtistInfo>> SearchArtistsAsync(string query)
    {
        await CloudArtistSearchHelper.SearchArtistsAsync(query, _artists, _api);
        return new CloudSearchPage<IBriefOnlineArtistInfo>(
            _artists.ToArray(),
            _artists.ArtistCount,
            !_artists.HasAllLoaded,
            _artists.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlineArtistInfo>> SearchMoreArtistsAsync()
    {
        var previousCount = _artists.Count;
        await CloudArtistSearchHelper.SearchMoreArtistsAsync(_artists, _api);
        return new CloudSearchPage<IBriefOnlineArtistInfo>(
            _artists.Skip(previousCount).ToArray(),
            _artists.ArtistCount,
            !_artists.HasAllLoaded && _artists.Count > previousCount,
            _artists.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlinePlaylistInfo>> SearchPlaylistsAsync(string query)
    {
        await CloudPlaylistSearchHelper.SearchPlaylistsAsync(query, _playlists, _api);
        return new CloudSearchPage<IBriefOnlinePlaylistInfo>(
            _playlists.ToArray(),
            _playlists.PlaylistCount,
            !_playlists.HasAllLoaded,
            _playlists.KeyWords
        );
    }

    public async Task<CloudSearchPage<IBriefOnlinePlaylistInfo>> SearchMorePlaylistsAsync()
    {
        var previousCount = _playlists.Count;
        await CloudPlaylistSearchHelper.SearchMorePlaylistsAsync(_playlists, _api);
        return new CloudSearchPage<IBriefOnlinePlaylistInfo>(
            _playlists.Skip(previousCount).ToArray(),
            _playlists.PlaylistCount,
            !_playlists.HasAllLoaded && _playlists.Count > previousCount,
            _playlists.KeyWords
        );
    }

    public Task<List<SuggestResult>> GetSuggestionsAsync(string query) =>
        CloudSuggestSearchHelper.GetSuggestAsync(query, _api);
}
