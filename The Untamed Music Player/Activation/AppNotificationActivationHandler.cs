using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace The_Untamed_Music_Player.Activation;

public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    public AppNotificationActivationHandler() { }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind
            == ExtendedActivationKind.AppNotification;
    }

    /// <summary>
    /// 处理程序已关闭时, 用户点击通知时的事件
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        /*App.MainWindow!.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            App.MainWindow.ShowMessageDialogAsync("TODO: Handle notification activations.", "Notification Activation");
        });*/

        await Task.CompletedTask;
    }
}
