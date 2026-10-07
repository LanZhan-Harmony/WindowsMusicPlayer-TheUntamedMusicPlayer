using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace UntamedMediaPlayer.Controls;

/// <summary>
/// 均衡器频段滑块。简化实现：支持 ThumbToolTip 值转换器的 Slider。
/// </summary>
public sealed partial class EqualizerBandSliderControl : Slider
{
    // The recovered UWP markup used Row directly.  Keep it as a compatibility
    // property and mirror it to Grid.Row for the WinUI layout system.
    public int Row
    {
        get => (int)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public static readonly DependencyProperty RowProperty = DependencyProperty.Register(
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

    public new IValueConverter? ThumbToolTipValueConverter
    {
        get => base.ThumbToolTipValueConverter;
        set => base.ThumbToolTipValueConverter = value;
    }

    public new static readonly DependencyProperty ThumbToolTipValueConverterProperty =
        Slider.ThumbToolTipValueConverterProperty;
}

/// <summary>
/// 将滑块增益值格式化为 "+N.N dB" / "-N.N dB"。
/// </summary>
public sealed partial class GainValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            return $"{d:+0.0;-0.0;0} dB";
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
