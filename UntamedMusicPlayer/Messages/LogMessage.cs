using Microsoft.Extensions.Logging;

namespace UntamedMusicPlayer.Messages;

/// <summary>
/// 日志消息，用于通过Messenger传递给UI显示
/// </summary>
/// <param name="level"></param>
/// <param name="message"></param>
public class LogMessage(LogLevel level, string message)
{
    public LogLevel Level { get; init; } = level;
    public string Message { get; init; } = message;
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
