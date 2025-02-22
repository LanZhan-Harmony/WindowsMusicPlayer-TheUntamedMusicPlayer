using System.Collections.Specialized;
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
        /*App.MainWindow!.DispatcherQueue.TryEnqueue(() =>
        {
            App.MainWindow.ShowMessageDialogAsync("TODO: Handle notification invocations when your app is already running.", "Notification Invoked");

            App.MainWindow.BringToFront();
        });*/
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
