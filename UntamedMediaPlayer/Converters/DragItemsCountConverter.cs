using Microsoft.UI.Xaml.Data;
using UntamedMediaPlayer.Strings;

namespace UntamedMediaPlayer.Converters;

internal sealed partial class DragItemsCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return Resources.DragItemsCountMessage(count);
        }
        return Resources.DragItemsCountMessage(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
