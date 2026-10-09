using Microsoft.UI.Xaml.Data;

namespace UntamedMediaPlayer.Converters;

/// <summary>
/// 将滑块增益值格式化为 "+N.N dB" / "-N.N dB"。
/// </summary>
internal sealed partial class GainValueConverter : IValueConverter
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
