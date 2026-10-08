using System.Collections;
using Microsoft.UI.Xaml.Data;

namespace UntamedMediaPlayer.Converters;

internal sealed partial class SortAndFilterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is IEnumerable && value is not string ? value : Array.Empty<object>();

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
}
