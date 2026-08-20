namespace UntamedMusicPlayer.Core.Messages;

public sealed class MusicLibraryReloadMessage(bool isReloading)
{
    public bool IsReloading { get; set; } = isReloading;
}
