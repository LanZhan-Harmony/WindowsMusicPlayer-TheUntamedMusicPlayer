namespace UntamedMusicPlayer.Core.Contracts.Models;

public interface IArtistInfoBase
{
    string Name { get; set; }
    string? CoverPath { get; set; }
}
