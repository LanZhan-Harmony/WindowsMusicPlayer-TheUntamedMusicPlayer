namespace UntamedMusicPlayer.Messages;

/// <summary>
/// 用于指示主题更改的消息
/// </summary>
/// <param name="isDarkTheme"></param>
public class ThemeChangeMessage(bool isDarkTheme)
{
    public bool IsDarkTheme { get; } = isDarkTheme;
}
