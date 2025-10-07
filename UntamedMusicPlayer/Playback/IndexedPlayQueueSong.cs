using MemoryPack;
using UntamedMusicPlayer.Contracts.Models;

namespace UntamedMusicPlayer.Playback;

[MemoryPackable]
public partial class IndexedPlayQueueSong
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
