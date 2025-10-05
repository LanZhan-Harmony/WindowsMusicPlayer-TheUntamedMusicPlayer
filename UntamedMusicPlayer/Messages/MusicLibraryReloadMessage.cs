namespace UntamedMusicPlayer.Messages;

public sealed class MusicLibraryReloadMessage(bool isReloading)
{
    public bool IsReloading { get; set; } = isReloading;
}
