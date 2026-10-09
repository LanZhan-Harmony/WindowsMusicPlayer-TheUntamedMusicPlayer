using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// 均衡器频段滑块。简化实现：支持 ThumbToolTip 值转换器的 Slider。
/// </summary>
internal sealed partial class EqualizerBandSliderControl : Slider
{
    // The recovered UWP markup used Row directly.  Keep it as a compatibility
    // property and mirror it to Grid.Row for the WinUI layout system.
    public int Row
    {
        get => (int)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    private static readonly DependencyProperty RowProperty = DependencyProperty.Register(
        nameof(Row),
        typeof(int),
        typeof(EqualizerBandSliderControl),
        new PropertyMetadata(0, OnRowChanged)
    );

    private static void OnRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EqualizerBandSliderControl slider)
        {
            Grid.SetRow(slider, (int)e.NewValue);
        }
    }
}
