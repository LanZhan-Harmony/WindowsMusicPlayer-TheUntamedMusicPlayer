#pragma warning disable

using System.Text;

namespace UntamedMusicPlayer.OnlineAPIs.CloudMusicAPI.Extensions;

internal static class ExceptionExtensions
{
    /// <summary>
    /// 获取最内层异常
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static Exception GetInmostException(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is null
            ? exception
            : exception.InnerException.GetInmostException();
    }

    /// <summary>
    /// 返回一个字符串，其中包含异常的所有信息。
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    public static string ToFullString(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sb = new StringBuilder();
        DumpException(exception, sb);
        return sb.ToString();
    }

    private static void DumpException(Exception exception, StringBuilder sb)
    {
        sb.AppendLine("Type: " + Environment.NewLine + exception.GetType().FullName);
        sb.AppendLine("Message: " + Environment.NewLine + exception.Message);
        sb.AppendLine("Source: " + Environment.NewLine + exception.Source);
        sb.AppendLine("StackTrace: " + Environment.NewLine + exception.StackTrace);
        sb.AppendLine("TargetSite: " + Environment.NewLine + $"{exception.TargetSite}");
        sb.AppendLine("----------------------------------------");
        if (exception.InnerException is not null)
        {
            DumpException(exception.InnerException, sb);
        }
    }
}
