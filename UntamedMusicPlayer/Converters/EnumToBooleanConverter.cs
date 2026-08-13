using Microsoft.UI.Xaml.Data;

namespace UntamedMusicPlayer.Converters;

public sealed partial class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            if (value is not Enum enumValue)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }
            var parsedValue = Enum.Parse(value.GetType(), enumString);
            return parsedValue.Equals(enumValue);
        }
        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            return Enum.Parse(targetType, enumString);
        }
        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }
}
