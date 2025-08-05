using Microsoft.UI.Xaml.Controls;
using The_Untamed_Music_Player.Contracts.Models;
using The_Untamed_Music_Player.Models;

namespace The_Untamed_Music_Player.Messages;

/// <summary>
/// Message requesting to scroll to a specific song in a ListView
/// </summary>
public sealed class ScrollToSongMessage
{
    public IBriefSongInfoBase? Song { get; } = Data.MusicPlayer.CurrentBriefSong;
    public ScrollIntoViewAlignment Alignment { get; } = ScrollIntoViewAlignment.Leading;
}
