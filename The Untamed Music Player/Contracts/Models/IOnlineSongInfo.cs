using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineSongInfo : IBriefSongInfoBase
{
    long ID { get; set; }
    long AlbumID { get; set; }

    new SolidColorBrush GetTextForeground(IDetailedSongInfoBase? currentSong, bool isDarkTheme)
    {
        var defaultColor = isDarkTheme ? Colors.White : Colors.Black;

        if (
            currentSong is not null
            && currentSong.IsOnline
            && ID == ((IDetailedOnlineSongInfo)currentSong).ID
        )
        {
            var highlightColor = isDarkTheme
                ? ColorHelper.FromArgb(0xFF, 0x42, 0x9C, 0xE3)
                : ColorHelper.FromArgb(0xFF, 0x00, 0x5A, 0x9E);
            return new SolidColorBrush(highlightColor);
        }
        return new SolidColorBrush(defaultColor);
    }
}

public interface IDetailedOnlineSongInfo : IBriefOnlineSongInfo, IDetailedSongInfoBase
{
    string? CoverUrl { get; set; }
}
