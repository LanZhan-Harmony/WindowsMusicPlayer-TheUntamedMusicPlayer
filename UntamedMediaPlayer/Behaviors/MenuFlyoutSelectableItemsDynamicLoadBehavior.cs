using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.Xaml.Interactivity;

namespace UntamedMediaPlayer.Behaviors;

/// <summary>
/// 为 SortAndFilterControl 动态加载可选菜单项，并通过 Converter 生成项。
/// 简化实现：保证控件具备可用的 Flyout。
/// </summary>
internal sealed class MenuFlyoutSelectableItemsDynamicLoadBehavior : Behavior<FrameworkElement>
{
    public IValueConverter? Converter
    {
        get => (IValueConverter?)GetValue(ConverterProperty);
        set => SetValue(ConverterProperty, value);
    }

    public static readonly DependencyProperty ConverterProperty = DependencyProperty.Register(
        nameof(Converter),
        typeof(IValueConverter),
        typeof(MenuFlyoutSelectableItemsDynamicLoadBehavior),
        new PropertyMetadata(null)
    );
}
