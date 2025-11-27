using Microsoft.UI.Xaml.Data;

namespace UntamedMusicPlayer.Helpers;

public sealed partial class DoubleToDecibelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double decibel)
        {
            var sign = decibel switch
            {
                > 0 => "+",
                < 0 => "-",
                _ => "",
            };
            var suffix = "EqualizerDialog_Decibel".GetLocalized();
            var formattedDecibel =
                Math.Abs(decibel) % 1 == 0
                    ? Math.Abs(decibel).ToString("F0")
                    : Math.Abs(decibel).ToString("F1");
            return $"{sign}{formattedDecibel} {suffix}";
        }
        throw new ArgumentException("将浮点数转换为分贝字符串失败");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
