namespace The_Untamed_Music_Player.Messages;

public sealed class MusicLibraryReloadMessage(bool isReloading)
{
    public bool IsReloading { get; set; } = isReloading;
}
