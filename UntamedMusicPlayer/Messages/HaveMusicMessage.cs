namespace UntamedMusicPlayer.Messages;

/// <summary>
/// 用于指示音乐库中是否有音乐的消息
/// </summary>
/// <param name="hasMusic"></param>
public sealed class HaveMusicMessage(bool hasMusic)
{
    public bool HasMusic { get; } = hasMusic;
}
