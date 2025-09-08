#pragma warning disable

using System.Text;
using The_Untamed_Music_Player;
using The_Untamed_Music_Player.OnlineAPIs;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;
using The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.Extensions;

internal static class ExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// 获取最内层异常
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public Exception GetInmostException()
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
        public string ToFullString()
        {
            ArgumentNullException.ThrowIfNull(exception);
            var sb = new StringBuilder();
            DumpException(exception, sb);
            return sb.ToString();
        }
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
