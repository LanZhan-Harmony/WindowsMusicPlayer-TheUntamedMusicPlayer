using UntamedMusicPlayer.Core.Models;

namespace UntamedMusicPlayer.Core.Contracts.Services;

/// <summary>
/// Invalidates presentation-level cover caches without coupling core services to a UI image type.
/// </summary>
public interface ICoverCacheInvalidationService
{
    void InvalidateAllSongCovers();

    void InvalidateAllPlaylistCovers();

    void InvalidatePlaylistCover(PlaylistInfo playlist);
}
