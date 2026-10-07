using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UntamedMediaPlayer.Converters;

internal sealed partial class EmptyObjectToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
