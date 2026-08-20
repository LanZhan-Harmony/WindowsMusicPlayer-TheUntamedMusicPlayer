using System.Collections.Concurrent;
using UntamedMusicPlayer.Core.Models;

namespace UntamedMusicPlayer.Core.Services;

/// <summary>
/// Thread-safe, UI-independent index for the local music library.
/// </summary>
/// <remarks>
/// The index publishes a complete state after each rebuild. Queries capture one
/// state before reading it, so they never observe aggregates while they are
/// being constructed.
/// </remarks>
public sealed class LocalLibraryIndex
{
    private LibraryState _state = LibraryState.CreateEmpty();

    public LocalLibraryIndex() { }

    public LocalLibraryIndex(IEnumerable<BriefLocalSongInfo> songs)
    {
        RebuildFromSongs(songs);
    }

    /// <summary>
    /// Gets the current songs as a read-only collection view.
    /// </summary>
    public IReadOnlyCollection<BriefLocalSongInfo> Songs => GetState().Songs;

    /// <summary>
    /// Gets whether the current snapshot contains at least one playable song.
    /// </summary>
    public bool HasSongs => !GetState().Songs.IsEmpty;

    /// <summary>
    /// Gets the current album index keyed by album name.
    /// </summary>
    public IReadOnlyDictionary<string, LocalAlbumInfo> Albums => GetState().Albums;

    /// <summary>
    /// Gets the current artist index keyed by artist name.
    /// </summary>
    public IReadOnlyDictionary<string, LocalArtistInfo> Artists => GetState().Artists;

    /// <summary>
    /// Gets the distinct, non-empty genre values found in the current songs.
    /// Presentation-only values such as an "all genres" option are not stored here.
    /// </summary>
    public IReadOnlyList<string> Genres => GetState().Genres;

    /// <summary>
    /// Replaces the complete index with a snapshot of the supplied songs.
    /// </summary>
    public void RebuildFromSongs(IEnumerable<BriefLocalSongInfo> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);

        var snapshot = SnapshotSongs(songs);
        var nextState = new LibraryState(
            new ConcurrentBag<BriefLocalSongInfo>(snapshot),
            BuildAlbumsFromSnapshot(snapshot),
            BuildArtistsFromSnapshot(snapshot),
            BuildGenresFromSnapshot(snapshot)
        );

        Volatile.Write(ref _state, nextState);
    }

    /// <summary>
    /// Builds album aggregates from a stable snapshot of the supplied songs.
    /// </summary>
    public static ConcurrentDictionary<string, LocalAlbumInfo> BuildAlbumsFromSongs(
        IEnumerable<BriefLocalSongInfo> songs
    )
    {
        ArgumentNullException.ThrowIfNull(songs);
        return BuildAlbumsFromSnapshot(SnapshotSongs(songs));
    }

    /// <summary>
    /// Builds artist aggregates from a stable snapshot of the supplied songs.
    /// </summary>
    public static ConcurrentDictionary<string, LocalArtistInfo> BuildArtistsFromSongs(
        IEnumerable<BriefLocalSongInfo> songs
    )
    {
        ArgumentNullException.ThrowIfNull(songs);
        return BuildArtistsFromSnapshot(SnapshotSongs(songs));
    }

    /// <summary>
    /// Builds the distinct genre values from a stable snapshot of the supplied songs.
    /// </summary>
    public static IReadOnlyList<string> BuildGenresFromSongs(
        IEnumerable<BriefLocalSongInfo> songs
    )
    {
        ArgumentNullException.ThrowIfNull(songs);
        return BuildGenresFromSnapshot(SnapshotSongs(songs));
    }

    /// <summary>
    /// Returns a copy of the current song collection.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsSnapshot()
    {
        var state = GetState();
        return state.Songs.ToArray();
    }

    /// <summary>
    /// Gets songs belonging to an album, ordered by title.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsByAlbum(LocalAlbumInfo localAlbumInfo)
    {
        ArgumentNullException.ThrowIfNull(localAlbumInfo);
        return GetSongsByAlbum(localAlbumInfo.Name);
    }

    /// <summary>
    /// Gets songs belonging to an album, ordered by title.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsByAlbum(string? albumName)
    {
        if (string.IsNullOrEmpty(albumName))
        {
            return [];
        }

        var state = GetState();
        return state
            .Songs.Where(song => string.Equals(song.Album, albumName, StringComparison.Ordinal))
            .OrderBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets the albums associated with an artist, ordered by album name.
    /// </summary>
    public LocalAlbumInfo[] GetAlbumsByArtist(LocalArtistInfo localArtistInfo)
    {
        ArgumentNullException.ThrowIfNull(localArtistInfo);
        return GetAlbumsByArtist(localArtistInfo.Name);
    }

    /// <summary>
    /// Gets the albums associated with an artist, ordered by album name.
    /// </summary>
    public LocalAlbumInfo[] GetAlbumsByArtist(string? artistName)
    {
        if (string.IsNullOrEmpty(artistName))
        {
            return [];
        }

        var state = GetState();
        return state
            .Songs.Where(song => ContainsArtist(song, artistName))
            .Select(song => song.Album)
            .Where(albumName => !string.IsNullOrEmpty(albumName))
            .Distinct(StringComparer.Ordinal)
            .Select(albumName => state.Albums.TryGetValue(albumName, out var album) ? album : null)
            .OfType<LocalAlbumInfo>()
            .OrderBy(album => album.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets songs associated with an artist, ordered by album and title.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsByArtist(LocalArtistInfo localArtistInfo)
    {
        ArgumentNullException.ThrowIfNull(localArtistInfo);
        return GetSongsByArtist(localArtistInfo.Name);
    }

    /// <summary>
    /// Gets songs associated with an artist, ordered by album and title.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsByArtist(string? artistName)
    {
        if (string.IsNullOrEmpty(artistName))
        {
            return [];
        }

        var state = GetState();
        return state
            .Songs.Where(song => ContainsArtist(song, artistName))
            .OrderBy(song => song.Album, StringComparer.OrdinalIgnoreCase)
            .ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets songs whose stored genre string exactly matches the supplied value.
    /// </summary>
    public BriefLocalSongInfo[] GetSongsByGenre(string? genre)
    {
        if (string.IsNullOrEmpty(genre))
        {
            return [];
        }

        var state = GetState();
        return state
            .Songs.Where(song => string.Equals(song.GenreStr, genre, StringComparison.Ordinal))
            .OrderBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Gets the album containing the supplied song.
    /// </summary>
    public LocalAlbumInfo? GetAlbumInfoBySong(BriefLocalSongInfo song)
    {
        ArgumentNullException.ThrowIfNull(song);
        return GetAlbumInfoBySong(song.Album);
    }

    /// <summary>
    /// Gets an album by its name.
    /// </summary>
    public LocalAlbumInfo? GetAlbumInfoBySong(string? albumName)
    {
        if (string.IsNullOrEmpty(albumName))
        {
            return null;
        }

        var state = GetState();
        return state.Albums.TryGetValue(albumName, out var album) ? album : null;
    }

    /// <summary>
    /// Gets the first indexed artist associated with the supplied song.
    /// </summary>
    public LocalArtistInfo? GetArtistInfoBySong(BriefLocalSongInfo song)
    {
        ArgumentNullException.ThrowIfNull(song);

        foreach (var artistName in song.Artists ?? [])
        {
            var artist = GetArtistInfoByName(artistName);
            if (artist is not null)
            {
                return artist;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets an artist by name.
    /// </summary>
    public LocalArtistInfo? GetArtistInfoBySong(string? artistName) => GetArtistInfoByName(artistName);

    private LocalArtistInfo? GetArtistInfoByName(string? artistName)
    {
        if (string.IsNullOrEmpty(artistName))
        {
            return null;
        }

        var state = GetState();
        return state.Artists.TryGetValue(artistName, out var artist) ? artist : null;
    }

    private LibraryState GetState() => Volatile.Read(ref _state);

    private static BriefLocalSongInfo[] SnapshotSongs(IEnumerable<BriefLocalSongInfo> songs)
    {
        return songs is ConcurrentBag<BriefLocalSongInfo> concurrentSongs
            ? concurrentSongs.ToArray()
            : songs.ToArray();
    }

    private static ConcurrentDictionary<string, LocalAlbumInfo> BuildAlbumsFromSnapshot(
        IReadOnlyList<BriefLocalSongInfo> songs
    )
    {
        var albums = new ConcurrentDictionary<string, LocalAlbumInfo>(StringComparer.Ordinal);

        foreach (var song in songs)
        {
            var albumName = song.Album ?? string.Empty;
            if (albums.TryGetValue(albumName, out var album))
            {
                album.Update(song);
                continue;
            }

            albums.TryAdd(albumName, new LocalAlbumInfo(song));
        }

        return albums;
    }

    private static ConcurrentDictionary<string, LocalArtistInfo> BuildArtistsFromSnapshot(
        IReadOnlyList<BriefLocalSongInfo> songs
    )
    {
        var artists = new ConcurrentDictionary<string, LocalArtistInfo>(StringComparer.Ordinal);

        foreach (var song in songs)
        {
            foreach (var artistName in song.Artists ?? [])
            {
                if (string.IsNullOrWhiteSpace(artistName))
                {
                    continue;
                }

                if (artists.TryGetValue(artistName, out var artist))
                {
                    artist.Update(song);
                    continue;
                }

                artists.TryAdd(artistName, new LocalArtistInfo(song, artistName));
            }
        }

        return artists;
    }

    private static IReadOnlyList<string> BuildGenresFromSnapshot(
        IReadOnlyList<BriefLocalSongInfo> songs
    )
    {
        return songs
            .Select(song => song.GenreStr)
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(genre => genre, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ContainsArtist(BriefLocalSongInfo song, string artistName)
    {
        return song.Artists?.Contains(artistName, StringComparer.Ordinal) == true;
    }

    private sealed class LibraryState
    {
        public ConcurrentBag<BriefLocalSongInfo> Songs { get; }
        public ConcurrentDictionary<string, LocalAlbumInfo> Albums { get; }
        public ConcurrentDictionary<string, LocalArtistInfo> Artists { get; }
        public IReadOnlyList<string> Genres { get; }

        public LibraryState(
            ConcurrentBag<BriefLocalSongInfo> songs,
            ConcurrentDictionary<string, LocalAlbumInfo> albums,
            ConcurrentDictionary<string, LocalArtistInfo> artists,
            IReadOnlyList<string> genres
        )
        {
            Songs = songs;
            Albums = albums;
            Artists = artists;
            Genres = genres;
        }

        public static LibraryState CreateEmpty() =>
            new(
                new ConcurrentBag<BriefLocalSongInfo>(),
                new ConcurrentDictionary<string, LocalAlbumInfo>(StringComparer.Ordinal),
                new ConcurrentDictionary<string, LocalArtistInfo>(StringComparer.Ordinal),
                Array.Empty<string>()
            );
    }
}
