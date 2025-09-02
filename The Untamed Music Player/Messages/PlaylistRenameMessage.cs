namespace The_Untamed_Music_Player.Messages;

/// <summary>
/// 用于指示某个播放列表重命名的消息
/// </summary>
/// <param name="playlist"></param>
public sealed class PlaylistRenameMessage(string oldName, string newName)
{
    public string OldName { get; } = oldName;
    public string NewName { get; } = newName;
}
