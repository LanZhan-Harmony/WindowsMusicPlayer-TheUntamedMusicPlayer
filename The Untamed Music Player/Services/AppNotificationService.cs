using System.Collections.Specialized;
using System.Diagnostics;
using System.Web;
using Microsoft.Windows.AppNotifications;
using The_Untamed_Music_Player.Contracts.Services;

namespace The_Untamed_Music_Player.Services;
public class AppNotificationService : IAppNotificationService
{
    public AppNotificationService() { }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        AppNotificationManager.Default.Register();
    }

    /// <summary>
    /// 处理程序运行时, 用户点击通知时的事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public static void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        var arguments = args.Arguments;
        if (arguments.TryGetValue("OpenFolderAction", out var savePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{savePath}\"",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }

    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);

        AppNotificationManager.Default.Show(appNotification);

        return appNotification.Id != 0;
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}
