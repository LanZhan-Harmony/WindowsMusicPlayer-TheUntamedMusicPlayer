using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// 响应式列表卡片项呈现器。简化实现：承载 ViewModel 与显示模式的内容控件。
/// </summary>
public sealed partial class ResponsiveListCardItemPresenter : ContentControl
{
    public object? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(object),
        typeof(ResponsiveListCardItemPresenter),
        new PropertyMetadata(null)
    );

    public string DisplayMode
    {
        get => (string)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
        nameof(DisplayMode),
        typeof(string),
        typeof(ResponsiveListCardItemPresenter),
        new PropertyMetadata(default(string))
    );
}
