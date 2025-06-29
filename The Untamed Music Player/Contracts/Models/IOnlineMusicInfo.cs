using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace The_Untamed_Music_Player.Contracts.Models;

public interface IBriefOnlineMusicInfo : IBriefMusicInfoBase
{
    long ID { get; set; }
    long AlbumID { get; set; }

    new SolidColorBrush GetTextForeground(IDetailedMusicInfoBase? currentMusic, bool isDarkTheme)
    {
        var defaultColor = isDarkTheme ? Colors.White : Colors.Black;

        if (
            currentMusic is not null
            && currentMusic.IsOnline
            && ID == ((IDetailedOnlineMusicInfo)currentMusic).ID
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

public interface IDetailedOnlineMusicInfo : IBriefOnlineMusicInfo, IDetailedMusicInfoBase
{
    string? CoverUrl { get; set; }
}
