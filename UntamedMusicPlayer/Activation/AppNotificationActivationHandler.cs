using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace UntamedMusicPlayer.Activation;

public sealed class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
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
        await Task.CompletedTask;
    }
}
