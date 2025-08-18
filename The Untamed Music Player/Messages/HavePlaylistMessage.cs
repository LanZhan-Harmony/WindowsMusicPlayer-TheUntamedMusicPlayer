namespace The_Untamed_Music_Player.Messages;

/// <summary>
/// 用于指示播放列表库中是否有播放列表的消息
/// </summary>
/// <param name="hasPlaylist"></param>
public sealed class HavePlaylistMessage(bool hasPlaylist)
{
    public bool HasPlaylist { get; } = hasPlaylist;
}
