using UntamedMusicPlayer.Core.Contracts.Services;
using UntamedMusicPlayer.Core.Models;

namespace UntamedMusicPlayer.Services;

public sealed class CoverCacheInvalidationService : ICoverCacheInvalidationService
{
    public void InvalidateAllSongCovers() => CoverManager.ForceAllSongCoversRefresh();

    public void InvalidateAllPlaylistCovers() => CoverManager.ForceAllPlaylistCoversRefresh();

    public void InvalidatePlaylistCover(PlaylistInfo playlist) =>
        CoverManager.ForcePlaylistCoverRefresh(playlist);
}
