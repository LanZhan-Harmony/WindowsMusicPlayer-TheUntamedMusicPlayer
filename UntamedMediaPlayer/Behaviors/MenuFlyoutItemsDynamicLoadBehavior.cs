using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// 在 AppBarButton 的 Flyout 打开时按需动态填充菜单项。
/// 简化实现：确保存在一个 MenuFlyout，实际项由 ViewModel/代码在 Opening 时填充。
/// </summary>
internal sealed class MenuFlyoutItemsDynamicLoadBehavior : Behavior<AppBarButton>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Flyout ??= new MenuFlyout();
    }
}
