using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Messages;

/// <summary>
/// 用于指示某个播放列表重命名的消息
/// </summary>
/// <param name="playlist"></param>
public sealed class PlaylistRenameMessage(string oldName, PlaylistInfo playlist)
{
    public string OldName { get; } = oldName;
    public PlaylistInfo Playlist { get; } = playlist;
}
