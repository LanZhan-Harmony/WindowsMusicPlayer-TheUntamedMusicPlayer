using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UntamedMediaPlayer.Converters;

internal sealed partial class EnumerationValueConverter : DependencyObject, IValueConverter
{
    public object TrueValue
    {
        get => GetValue(TrueValueProperty);
        set => SetValue(TrueValueProperty, value);
    }

    private static readonly DependencyProperty TrueValueProperty = DependencyProperty.Register(
        nameof(TrueValue),
        typeof(object),
        typeof(EnumerationValueConverter),
        new PropertyMetadata(true)
    );

    public object FalseValue
    {
        get => GetValue(FalseValueProperty);
        set => SetValue(FalseValueProperty, value);
    }

    private static readonly DependencyProperty FalseValueProperty = DependencyProperty.Register(
        nameof(FalseValue),
        typeof(object),
        typeof(EnumerationValueConverter),
        new PropertyMetadata(false)
    );

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return Equals(value, parameter) ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
