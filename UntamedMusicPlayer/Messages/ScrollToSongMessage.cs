using Microsoft.UI.Xaml.Controls;
using UntamedMusicPlayer.Contracts.Models;
using UntamedMusicPlayer.Models;

namespace UntamedMusicPlayer.Messages;

/// <summary>
/// 用于 ListView 滚动到特定歌曲的消息
/// </summary>
public sealed class ScrollToSongMessage
{
    public IBriefSongInfoBase? Song { get; } = Data.PlayState.CurrentBriefSong;
    public ScrollIntoViewAlignment Alignment { get; } = ScrollIntoViewAlignment.Leading;
}
