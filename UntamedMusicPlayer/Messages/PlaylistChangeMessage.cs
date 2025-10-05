using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Messages;

/// <summary>
/// 用于指示某个播放列表变化的消息
/// </summary>
/// <param name="playlist"></param>
public sealed class PlaylistChangeMessage(PlaylistInfo playlist)
{
    public PlaylistInfo Playlist { get; } = playlist;
}
