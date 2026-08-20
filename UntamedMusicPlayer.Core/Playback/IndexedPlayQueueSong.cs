using MemoryPack;
using UntamedMusicPlayer.Core.Contracts.Models;

namespace UntamedMusicPlayer.Core.Playback;

[MemoryPackable]
public sealed partial class IndexedPlayQueueSong
{
    public int Index { get; set; }
    public IBriefSongInfoBase Song { get; set; } = null!;

    [MemoryPackConstructor]
    public IndexedPlayQueueSong() { }

    public IndexedPlayQueueSong(int index, IBriefSongInfoBase song)
    {
        Index = index;
        Song = song;
    }
}
