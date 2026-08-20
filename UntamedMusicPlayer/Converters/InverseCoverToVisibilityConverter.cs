using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UntamedMusicPlayer.Converters;

public sealed partial class InverseCoverToVisibilityConverter : IValueConverter
{
    private static readonly CoverConverter _coverConverter = new();

    public object Convert(object value, Type targetType, object parameter, string language) =>
        _coverConverter.Convert(value, targetType, parameter, language) is null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
